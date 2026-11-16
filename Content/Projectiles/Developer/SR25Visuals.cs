using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Developer;

// Complete, bounded artwork only: no MagicPixel, stretched texels or world-length strips.
internal static class SR25Visuals
{
    private static readonly Dictionary<string, Asset<Texture2D>> textures = new();
    internal static Texture2D Art(string name)
    {
        if (!textures.TryGetValue(name, out Asset<Texture2D> asset))
            textures[name] = asset = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/SR25/" + name,
                AssetRequestMode.ImmediateLoad);
        // Keep the asset handle, never the temporary blank texture returned during async loading.
        return asset.Value;
    }
    internal static void Clear() => textures.Clear();
    internal static float VisualRange => Math.Min(460f, Main.screenWidth * .32f / Math.Max(.5f, Main.GameViewMatrix.Zoom.X));
    private static void Fan(string name, Vector2 center, float angle, float scale, float opacity)
        => Main.EntitySpriteDraw(Art(name), center - Main.screenPosition, null, Color.White * opacity,
            angle, new Vector2(8, 140), scale, SpriteEffects.None);

    internal static void DrawScan(Vector2 center, Vector2 aim, Vector2? target = null)
    {
        float time = Main.GlobalTimeWrappedHourly, range = VisualRange, angle = aim.ToRotation();
        Fan("RadarField", center, angle, range / 500f, .65f);
        float sweep = MathF.Sin(time * 1.8f) * MathHelper.ToRadians(12.5f);
        Fan("RadarSweep", center, angle + sweep, range / 500f, .6f);
        for (int i = 0; i < 2; i++)
        {
            float phase = (time * .65f + i * .5f) % 1f;
            Fan("RadarPulse", center, angle, range / 500f * (.12f + .88f * phase), (1 - phase) * .55f);
        }
        // A small personal PPI reports direction/range without painting over the world.
        Vector2 dial = center + new Vector2(-58, -76);
        Main.EntitySpriteDraw(Art("RadarDial"), dial - Main.screenPosition, null, Color.White * .78f,
            0, new Vector2(48), 1, SpriteEffects.None);
        Fan("RadarSweep", dial, angle + sweep, 42f / 500, .55f);
        if (target.HasValue)
        {
            Vector2 point = dial + (target.Value - center) / SR25LockSystem.ScanRange * 40;
            float pulse = .65f + .35f * MathF.Pow(MathF.Sin(time * 5), 2);
            Main.EntitySpriteDraw(Art("RadarEcho"), point - Main.screenPosition, null, Color.White * pulse,
                0, new Vector2(16), .33f, SpriteEffects.None);
        }
    }
    internal static void DrawMuzzle(Vector2 position, float angle, float opacity)
    {
        if (opacity < .04f) return;
        Texture2D flash = Art("MuzzleFlash");
        int height = flash.Height / 3, frame = Math.Clamp((int)((1 - opacity) * 3), 0, 2);
        Main.EntitySpriteDraw(flash, position - Main.screenPosition, new Rectangle(0, frame * height, flash.Width, height),
            Color.White * opacity, angle, new Vector2(0, height / 2f), .36f, SpriteEffects.None);
    }
    internal static void DrawPassiveRadar(Vector2 center, Vector2? target)
    {
        float time = Main.GlobalTimeWrappedHourly, radius = Math.Min(320, VisualRange * .7f);
        Main.EntitySpriteDraw(Art("RadarCircle"), center - Main.screenPosition, null, Color.White * .7f,
            0, new Vector2(256), radius / 250, SpriteEffects.None);
        Fan("RadarSweep", center, time * 1.6f, radius / 500, .65f);
        for (int i = 0; i < 2; i++)
        {
            float phase = (time * .55f + i * .5f) % 1;
            Main.EntitySpriteDraw(Art("RadarCirclePulse"), center - Main.screenPosition, null, Color.White * ((1 - phase) * .38f),
                0, new Vector2(256), radius / 246 * (.1f + .9f * phase), SpriteEffects.None);
        }
        Vector2 dial = center + new Vector2(-58, -76);
        Main.EntitySpriteDraw(Art("RadarDial"), dial - Main.screenPosition, null, Color.White * .82f,
            0, new Vector2(48), 1, SpriteEffects.None);
        Fan("RadarSweep", dial, time * 1.6f, 42f / 500, .7f);
        if (target.HasValue)
        {
            Vector2 point = dial + (target.Value - center) / SR25LockSystem.ScanRange * 40;
            Main.EntitySpriteDraw(Art("RadarEcho"), point - Main.screenPosition, null, Color.White,
                0, new Vector2(16), .33f, SpriteEffects.None);
        }
    }
    internal static void DrawReticle(NPC npc)
    {
        float time = Main.GlobalTimeWrappedHourly;
        Texture2D reticle = Art("TargetReticle");
        float diameter = MathHelper.Clamp(Math.Max(npc.width, npc.height) + 24, 56, 156);
        float scale = diameter / reticle.Width * (1 + .025f * MathF.Sin(time * 4));
        Main.EntitySpriteDraw(reticle, npc.Center - Main.screenPosition, null, Color.White * .9f,
            0, reticle.Size() / 2, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(Art("RadarEcho"), npc.Center - Main.screenPosition, null,
            Color.White * (.28f + .15f * MathF.Sin(time * 4)), 0, new Vector2(16), .4f, SpriteEffects.None);
    }
}

public sealed class SR25VisualAssets : ModSystem
{
    public override void Unload() => SR25Visuals.Clear();
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Player player = Main.LocalPlayer;
        var radar = player.GetModPlayer<SR25Player>();
        if (!radar.Alive || !radar.RadarEquipped || radar.HideRadar) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        SR25Visuals.DrawPassiveRadar(player.MountedCenter, radar.Target?.Center);
        Main.spriteBatch.End();
    }
}
