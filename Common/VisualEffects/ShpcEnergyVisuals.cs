using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Phalanx;
using ArknightsMod.Content.Projectiles.Scatterguns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Local SHPC/Calamity material port. Both weapon lines share one render pass;
// no Calamity or Legends assembly is required and no global dye state is changed.
public sealed class ShpcEnergyVisuals : ModSystem
{
    internal const string Assets = "ArknightsMod/Content/Textures/ShpcEnergy/";
    private static Effect trail;
    private readonly List<Projectile> visible = new();
    private static BlendState glowBlend;
    private static BlendState GlowBlend => glowBlend ??= new() {
        ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One
    };
    public override void Unload()
    {
        Effect old = trail; BlendState oldBlend = glowBlend; trail = null; glowBlend = null;
        Main.QueueMainThreadAction(() => { old?.Dispose(); oldBlend?.Dispose(); });
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        visible.Clear();
        foreach (Projectile p in Main.ActiveProjectiles)
            if ((p.ModProjectile is ChalterSplitOrb || p.ModProjectile is PhalanxChargeSoul)
                && OperatorFlightVfx.Finite(p.Center)
                && p.Center.X > Main.screenPosition.X - 420 && p.Center.X < Main.screenPosition.X + Main.screenWidth + 420
                && p.Center.Y > Main.screenPosition.Y - 420 && p.Center.Y < Main.screenPosition.Y + Main.screenHeight + 420)
                visible.Add(p);
        if (visible.Count == 0) return;
        var device = Main.instance.GraphicsDevice;
        trail ??= new Effect(device, Mod.GetFileBytes("Assets/Effects/ShpcEnergy/SoulTrail.fxc"));
        BlendState oldBlend = device.BlendState;
        RasterizerState oldRaster = device.RasterizerState;
        DepthStencilState oldDepth = device.DepthStencilState;
        Texture oldTexture = device.Textures[1];
        SamplerState oldSampler = device.SamplerStates[1];
        try
        {
            device.BlendState = GlowBlend;
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.None;
            device.SamplerStates[1] = SamplerState.LinearWrap;
            trail.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            trail.Parameters["uWorldViewProjection"].SetValue(Main.GameViewMatrix.TransformationMatrix
                * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            foreach (Projectile p in visible)
            {
                if (p.ModProjectile is ChalterSplitOrb orb)
                    DrawTrail(device, p, orb.Tint, 18, p.oldPos.Length, "SylvestaffStreak", true);
                else if (p.ModProjectile is PhalanxChargeSoul soul)
                {
                    DrawTrail(device, p, soul.Tint, 24 * p.scale, p.oldPos.Length, "ScarletDevilStreak", false);
                    DrawTrail(device, p, Color.Lerp(soul.Tint, Color.White, .85f), 14 * p.scale, 8, "SylvestaffStreak", false);
                }
            }
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            device.Textures[1] = oldTexture; device.SamplerStates[1] = oldSampler;
            device.BlendState = oldBlend; device.RasterizerState = oldRaster; device.DepthStencilState = oldDepth;
        }
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Projectile p in visible)
                if (p.ModProjectile is ChalterSplitOrb orb) orb.DrawCore();
        }
        finally { Main.spriteBatch.End(); }
    }
    private static void DrawTrail(GraphicsDevice device, Projectile p, Color tint, float width, int limit, string material, bool streak)
        => DrawRibbon(device, trail, ModContent.Request<Texture2D>(Assets + material).Value, p, tint, width, limit, streak);
    internal static void DrawRibbon(GraphicsDevice device, Effect effect, Texture2D texture, Projectile p,
        Color tint, float width, int limit, bool streak)
    {
        var points = new List<Vector2>(20) { p.Center };
        for (int i = 0; i < Math.Min(limit, p.oldPos.Length); i++)
        {
            Vector2 saved = p.oldPos[i];
            if (saved == Vector2.Zero || !OperatorFlightVfx.Finite(saved)) break;
            Vector2 point = saved + p.Size * .5f;
            float step = Vector2.Distance(points[^1], point);
            if (step > 96) break;
            if (step > .1f) points.Add(point);
        }
        if (points.Count < 3) return;
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(points.Count - 1) * 6];
        Vector2 previousNormal = Vector2.UnitY;
        int index = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 normal = (points[i + 1] - points[i]).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Vector2 n0 = i == 0 ? normal : (previousNormal + normal).SafeNormalize(normal);
            previousNormal = normal;
            float t0 = i / (points.Count - 1f), t1 = (i + 1) / (points.Count - 1f);
            float w0 = RibbonWidth(t0, width, streak), w1 = RibbonWidth(t1, width, streak);
            float fade = p.ModProjectile is PhalanxChargeSoul ? p.Opacity : 1;
            Color a = tint * ((1 - t0) * fade), b = tint * ((1 - t1) * fade);
            Vector2 c0 = points[i] - Main.screenPosition, c1 = points[i + 1] - Main.screenPosition;
            var v0 = new VertexPositionColorTexture(new Vector3(c0 - n0 * w0, 0), a, new Vector2(t0, 0));
            var v1 = new VertexPositionColorTexture(new Vector3(c0 + n0 * w0, 0), a, new Vector2(t0, 1));
            var v2 = new VertexPositionColorTexture(new Vector3(c1 - normal * w1, 0), b, new Vector2(t1, 0));
            var v3 = new VertexPositionColorTexture(new Vector3(c1 + normal * w1, 0), b, new Vector2(t1, 1));
            vertices[index++] = v0; vertices[index++] = v1; vertices[index++] = v2;
            vertices[index++] = v2; vertices[index++] = v1; vertices[index++] = v3;
        }
        effect.Parameters["uStreakOnly"].SetValue(streak ? 1f : 0f);
        effect.Parameters["uStreak"].SetValue(texture);
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
        }
    }
    internal static float RibbonWidth(float t, float width, bool streak) => streak
        ? MathF.Sin((1 - t) * MathHelper.PiOver2) * width
        : t < .15f ? MathF.Sin(t / .15f * MathHelper.PiOver2) * width + .15f : width * (1 - t) / .85f;
    internal static void Bloom(Vector2 point, Vector2 size, Color color, float rotation = 0)
    {
        color.A = 0;
        PhalanxVisuals.Sprite(ModContent.Request<Texture2D>(Assets + "BloomCircle").Value,
            point - Main.screenPosition, size, color, rotation);
    }
}
