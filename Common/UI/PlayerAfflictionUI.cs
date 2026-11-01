using System.Collections.Generic;
using System.Globalization;
using ArknightsMod.Content.ElementalImpairment.Effect;
using ArknightsMod.Content.Items.Armor.Specialist.Mortis;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Common.UI;

public sealed class PlayerAfflictionUI : ModSystem
{
    private readonly bool[] wasBursting = new bool[Main.maxPlayers];

    public override void OnWorldLoad() => System.Array.Clear(wasBursting);
    public override void OnWorldUnload() => System.Array.Clear(wasBursting);

    public override void PostUpdatePlayers() {
        if (Main.dedServ || Main.gameMenu) return;
        for (int i = 0; i < Main.maxPlayers; i++) {
            Player player = Main.player[i];
            bool bursting = player.active && !player.dead && player.GetModPlayer<NecrosisPlayer>().IsBursting;
            if (bursting && !wasBursting[i]) AfflictionIndicatorRenderer.PlayBurst(player);
            wasBursting[i] = bursting;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
        if (index < 0) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("ArknightsMod: Player Afflictions",
            DrawIndicators, InterfaceScaleType.None));
        layers.Insert(index + 1, new LegacyGameInterfaceLayer("ArknightsMod: Substitute Countdown",
            DrawSubstituteCountdowns, InterfaceScaleType.None));
    }

    private static bool DrawSubstituteCountdowns() {
        if (Main.gameMenu || Main.dedServ) return true;
        var font = FontAssets.MouseText.Value;
        float scale = .8f * Main.GameViewMatrix.Zoom.X;
        foreach (Player player in Main.ActivePlayers) {
            if (player.dead || player.ghost) continue;
            int remaining = player.GetModPlayer<MortisSetPlayer>().SubstituteTime;
            if (remaining <= 0) continue;
            Vector2 position = Vector2.Transform(player.Top - new Vector2(0, 16f) - Main.screenPosition,
                Main.GameViewMatrix.TransformationMatrix);
            if (position.X < -100 || position.X > Main.screenWidth + 100
                || position.Y < -100 || position.Y > Main.screenHeight + 100) continue;
            // Round upward so an active substitute never displays 0.0 seconds.
            string text = (System.Math.Ceiling(remaining / 6d) / 10d).ToString("0.0", CultureInfo.InvariantCulture) + "s";
            Utils.DrawBorderStringFourWay(Main.spriteBatch, font, text, position.X, position.Y,
                new Color(220, 210, 255), Color.Black, font.MeasureString(text) * new Vector2(.5f, 1f), scale);
        }
        return true;
    }

    private static bool Visible(Player player) {
        if (!player.active || player.dead || player.ghost) return false;
        var state = player.GetModPlayer<NecrosisPlayer>();
        if (state.Accumulation <= 0 && !state.IsBursting) return false;
        Vector2 position = ScreenPosition(player);
        return position.X >= -100 && position.X <= Main.screenWidth + 100
            && position.Y >= -100 && position.Y <= Main.screenHeight + 100;
    }

    private static Vector2 ScreenPosition(Player player) => Vector2.Transform(
        player.Bottom + new Vector2(0, 5f) - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);

    private static bool DrawIndicators() {
        if (Main.gameMenu || Main.dedServ) return true;
        bool anyVisible = false;
        foreach (Player player in Main.ActivePlayers)
            if (Visible(player)) { anyVisible = true; break; }
        if (!anyVisible) return true;

        SpriteBatch spriteBatch = Main.spriteBatch;
        float scale = Main.GameViewMatrix.Zoom.X;
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        foreach (Player player in Main.ActivePlayers) {
            if (!Visible(player)) continue;
            AfflictionIndicatorRenderer.DrawIcon(spriteBatch, AfflictionIndicatorRenderer.NecrosisStyle,
                ScreenPosition(player), scale);
        }
        spriteBatch.End();

        foreach (Player player in Main.ActivePlayers) {
            if (!Visible(player)) continue;
            var state = player.GetModPlayer<NecrosisPlayer>();
            // Match the enemy ring: accumulation consumes the white ring;
            // a grey ring refills as the burst expires.
            float progress = state.IsBursting ? 1f - state.BurstTime / (float)NecrosisPlayer.BurstDuration
                : 1f - state.Accumulation / (float)NecrosisPlayer.Threshold;
            AfflictionIndicatorRenderer.DrawRing(ScreenPosition(player), scale, progress, state.IsBursting);
        }
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        return true;
    }
}
