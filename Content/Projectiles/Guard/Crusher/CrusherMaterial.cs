using System;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherMaterial
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] trail = new VertexPositionColorTexture[52];
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[4];
    internal static void Unload() => material = null;
    internal static void DrawTrail(Vector2[] outer, Vector2[] inner, int samples, CrusherKind kind, float opacity)
    {
        if (Main.dedServ || opacity <= .005f || samples < 2) return;
        int count = Math.Min(samples, 26);
        for (int i = 0; i < count; i++) {
            if (!float.IsFinite(outer[i].X) || !float.IsFinite(outer[i].Y) || Vector2.DistanceSquared(outer[0], outer[i]) > 720 * 720) { count = i; break; }
            Vector2 rim = outer[i] + (outer[i] - inner[i]).SafeNormalize(Vector2.UnitX) * 12;
            trail[i * 2] = new(new Vector3(rim - Main.screenPosition, 0), Color.White, new(i / (float)Math.Max(1, samples - 1), 0));
            trail[i * 2 + 1] = new(new Vector3(inner[i] - Main.screenPosition, 0), Color.White, new(i / (float)Math.Max(1, samples - 1), 1));
        }
        if (count >= 2) Draw(trail, count * 2, "TrailPass", kind, Main.GlobalTimeWrappedHourly, opacity);
    }
    internal static void DrawSeal(Vector2 point, CrusherKind kind, float opacity, float charge, float gravity)
    {
        if (!CrusherVisuals.Visible(point)) return;
        Quad(point, new Vector2(72 + charge * 12, 23 + charge * 4), 0);
        Draw(quad, 4, "SealPass", kind, Main.GlobalTimeWrappedHourly, opacity * (.6f + charge * .35f));
    }
    internal static void DrawImpact(Vector2 point, Vector2 direction, CrusherKind kind, float progress, float strength)
    {
        if (!CrusherVisuals.Visible(point)) return;
        Quad(point, new Vector2(105, 77) * strength, direction.ToRotation());
        Draw(quad, 4, "ImpactPass", kind, progress, 1);
    }
    internal static void DrawAnchorExplosion(Vector2 point, float progress)
    {
        if (!CrusherVisuals.Visible(point)) return;
        Quad(point, new Vector2(110), 0);
        Draw(quad, 4, "AnchorBlastPass", CrusherKind.Ulpianus, progress, 1);
    }
    private static void Quad(Vector2 point, Vector2 size, float angle)
    {
        Vector2 x = angle.ToRotationVector2() * size.X, y = (angle + MathHelper.PiOver2).ToRotationVector2() * size.Y;
        quad[0] = new(new Vector3(point - x - y - Main.screenPosition, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(point + x - y - Main.screenPosition, 0), Color.White, new(1, 0));
        quad[2] = new(new Vector3(point - x + y - Main.screenPosition, 0), Color.White, new(0, 1));
        quad[3] = new(new Vector3(point + x + y - Main.screenPosition, 0), Color.White, new(1, 1));
    }
    private static void Draw(VertexPositionColorTexture[] mesh, int count, string pass, CrusherKind kind, float phase, float opacity)
    {
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CrusherMantle", AssetRequestMode.ImmediateLoad).Value;
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["BodyTint"].SetValue(CrusherRules.Tint(kind).ToVector3()); material.Parameters["EdgeTint"].SetValue(CrusherRules.Edge(kind).ToVector3());
        material.Parameters["Phase"].SetValue(phase); material.Parameters["Style"].SetValue((float)kind); material.Parameters["Opacity"].SetValue(Math.Clamp(opacity, 0, 1));
        var device = Main.spriteBatch.GraphicsDevice; Main.spriteBatch.End();
        try {
            device.BlendState = BlendState.NonPremultiplied; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes[pass].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, mesh, 0, count - 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
public sealed class CrusherVisualSystem : ModSystem
{
    public override void Unload() { CrusherMaterial.Unload(); CrusherWaveMaterial.Unload(); CrusherUpheavalVisuals.Unload(); CrusherVisuals.Unload(); }
}
