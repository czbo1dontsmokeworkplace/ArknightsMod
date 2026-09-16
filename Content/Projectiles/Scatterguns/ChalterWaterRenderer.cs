using System;
using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

// Original water lens: analytic droplet mask, flowing caustics and compact foam flecks.
// Like Fran's material renderers, texture dimensions are normalized and effects are client-only.
public sealed class ChalterWaterRenderer : ModSystem
{
    private static Effect waterShader;
    private static bool shaderFailed;
    public override void Unload()
    {
        Effect old = waterShader; waterShader = null; shaderFailed = false;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
    public static Color[] BuildOriginalTexture(int width, int height)
    {
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float u = (x + .5f) / width * 2 - 1;
            float v = (y + .5f) / height * 2 - 1;
            float halfWidth = .32f + .43f * (u + 1) * .5f;
            float bend = MathF.Sin(u * 5) * .055f * (1 - u);
            float radius = MathF.Sqrt(u * u / .94f / .94f + (v - bend) * (v - bend) / (halfWidth * halfWidth));
            float alpha = Math.Clamp((1 - radius) / .16f, 0, 1);
            float ripple = MathF.Pow(Math.Max(0, MathF.Sin(u * 19 + v * 9 + MathF.Sin(v * 13))), 6);
            float rim = MathF.Exp(-MathF.Pow((radius - .8f) * 12, 2));
            Color pigment = Color.Lerp(new Color(16, 105, 178), new Color(120, 237, 251),
                Math.Clamp(.25f + rim * .6f + ripple * .25f, 0, 1));
            pixels[y * width + x] = pigment * alpha; // Premultiplied alpha for SpriteBatch.
        }
        return pixels;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (IsVisibleWater(p)) { any = true; break; }
        if (!any) return;
        Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Scatterguns/ChalterWaterLens").Value;
        if (!shaderFailed && waterShader == null)
        {
            try { waterShader = new Effect(Main.instance.GraphicsDevice, Mod.GetFileBytes("Assets/Effects/ChalterWater.fxc")); }
            catch (Exception e) { shaderFailed = true; Mod.Logger.Warn("Water material unavailable; using original texture. " + e); }
        }
        waterShader?.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
        // All water pellets share this batch instead of restarting SpriteBatch for every pellet.
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, waterShader, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (!IsVisibleWater(p)) continue;
                Vector2 center = p.Center - Main.screenPosition;
                for (int i = 1; i < Math.Min(4, p.oldPos.Length); i++)
                {
                    Vector2 old = p.oldPos[i];
                    if (old == Vector2.Zero || !float.IsFinite(old.X) || !float.IsFinite(old.Y)
                        || Vector2.DistanceSquared(old, p.position) > 48 * 48) continue;
                    PhalanxVisuals.Sprite(texture, old + p.Size * .5f - Main.screenPosition,
                        new Vector2(42, 22) * (1 - i * .08f), Color.White * ((1 - i / 4f) * .22f), p.rotation);
                }
                PhalanxVisuals.Sprite(texture, center, new Vector2(42, 22), Color.White * .94f, p.rotation);
            }
        }
        finally { Main.spriteBatch.End(); }
    }
    private static bool IsVisibleWater(Projectile p) => p.ModProjectile is ScatterPellet && p.ai[0] == 2
        && float.IsFinite(p.Center.X) && float.IsFinite(p.Center.Y)
        && p.Center.X > Main.screenPosition.X - 100 && p.Center.X < Main.screenPosition.X + Main.screenWidth + 100
        && p.Center.Y > Main.screenPosition.Y - 100 && p.Center.Y < Main.screenPosition.Y + Main.screenHeight + 100;
}
