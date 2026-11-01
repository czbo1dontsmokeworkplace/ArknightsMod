using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.ElementalImpairment.Effect;

// Shared by enemy afflictions and player afflictions; callers own the SpriteBatch state.
internal static class AfflictionIndicatorRenderer
{
    internal static readonly NecrosisImpairment NecrosisStyle = new();

    internal static void DrawIcon(SpriteBatch spriteBatch, ElementalAffliction style, Vector2 position, float scale) {
        Texture2D feather = ModContent.Request<Texture2D>(style.FeatherMaskTexture).Value;
        Texture2D icon = ModContent.Request<Texture2D>(style.IconMaskTexture).Value;
        spriteBatch.Draw(feather, position, null, style.FeatherColor, 0f, feather.Size() * .5f,
            style.FeatherScale * scale, SpriteEffects.None, 0);
        spriteBatch.Draw(icon, position, null, style.IconColor, 0f, icon.Size() * .5f,
            style.MainScale * scale, SpriteEffects.None, 0);
    }

    internal static void DrawRing(Vector2 position, float scale, float progress, bool bursting) {
        Color color = bursting ? new Color(165, 165, 165, 180) : Color.White;
        AfflictionGlobalNPC.RingDrawer.DrawRing(position, 5f * scale, 2.5f * scale,
            MathHelper.Clamp(progress, 0f, 1f), color, 70);
    }

    internal static void PlayBurst(Player player) {
        var style = NecrosisStyle;
        BurstFlashEffect.Play(player, player.Bottom + new Vector2(0, 5f),
            style.BurstFlashMainMask, style.BurstFlashFeatherMask,
            style.BurstFlashMainColor, style.BurstFlashFeatherColor);
    }
}
