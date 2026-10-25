using System;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherWaveMaterial
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[4];
    private static readonly VertexPositionColorTexture[] ribbon = new VertexPositionColorTexture[36];
    internal const float CoreWidthMultiplier = 2f;
    internal static void Unload() => material = null;
    internal static void DrawRim(Projectile projectile, CrusherKind kind)
    {
        Quad(projectile.Center, new Vector2(112) * projectile.scale, projectile.rotation);
        Draw(quad, 4, "WavePass", kind, projectile.Opacity * .65f);
    }
    internal static void DrawCore(Projectile projectile, Vector2[] history, int samples, CrusherKind kind)
    {
        if(!CrusherVisuals.Visible(projectile.Center))return;
        DrawHistory(projectile,history,samples,kind,5*CoreWidthMultiplier);
        Quad(projectile.Center, new Vector2(23, 15 * CoreWidthMultiplier), projectile.rotation);
        Draw(quad, 4, "CorePass", kind, projectile.Opacity);
    }
    internal static void DrawAnchorTrail(Projectile projectile,Vector2[] history,int samples)
        => DrawHistory(projectile,history,samples,CrusherKind.Ulpianus,13);
    internal static void DrawUpheavalTrail(Projectile projectile, Vector2[] history, int samples, CrusherKind kind)
        => DrawHistory(projectile, history, samples, kind, 24 + (int)kind * 8);
    private static void DrawHistory(Projectile projectile,Vector2[] history,int samples,CrusherKind kind,float halfWidth)
    {
        if (!CrusherVisuals.Visible(projectile.Center)) return;
        int count = Math.Min(samples, 18);
        for (int i = 0; i < count; i++) {
            if (!float.IsFinite(history[i].X) || !float.IsFinite(history[i].Y)
                || Vector2.DistanceSquared(projectile.Center, history[i]) > 650 * 650) { count = i; break; }
            Vector2 tangent = i + 1 < count ? history[i] - history[i + 1] : projectile.velocity;
            Vector2 side = tangent.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float u = i / (float)Math.Max(1, count - 1), width = halfWidth * (1 - u) * projectile.Opacity;
            ribbon[i * 2] = new(new Vector3(history[i] + side * width - Main.screenPosition, 0), Color.White, new(u, 0));
            ribbon[i * 2 + 1] = new(new Vector3(history[i] - side * width - Main.screenPosition, 0), Color.White, new(u, 1));
        }
        if (count > 1) Draw(ribbon, count * 2, "TrailPass", kind, projectile.Opacity);
    }
    private static void Quad(Vector2 center, Vector2 half, float angle)
    {
        Vector2 x = angle.ToRotationVector2() * half.X, y = (angle + MathHelper.PiOver2).ToRotationVector2() * half.Y;
        quad[0] = new(new Vector3(center - x - y - Main.screenPosition, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(center + x - y - Main.screenPosition, 0), Color.White, new(1, 0));
        quad[2] = new(new Vector3(center - x + y - Main.screenPosition, 0), Color.White, new(0, 1));
        quad[3] = new(new Vector3(center + x + y - Main.screenPosition, 0), Color.White, new(1, 1));
    }
    private static void Draw(VertexPositionColorTexture[] mesh, int count, string pass, CrusherKind kind, float opacity)
    {
        if (Main.dedServ || opacity <= .005f) return;
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CrusherWave", AssetRequestMode.ImmediateLoad).Value;
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["BodyTint"].SetValue(CrusherRules.Tint(kind).ToVector3());
        material.Parameters["EdgeTint"].SetValue(CrusherRules.Edge(kind).ToVector3());
        material.Parameters["Phase"].SetValue(Main.GlobalTimeWrappedHourly); material.Parameters["Style"].SetValue((float)kind);
        material.Parameters["Opacity"].SetValue(opacity);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice; Main.spriteBatch.End();
        try {
            device.BlendState = BlendState.Additive; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes[pass].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, mesh, 0, count - 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
