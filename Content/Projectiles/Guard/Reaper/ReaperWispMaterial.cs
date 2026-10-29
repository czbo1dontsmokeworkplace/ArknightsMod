using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

// Archived 2026-10-28: retained with its shader and particle types. Normal
// ReaperWisp flight/impact no longer enters these drawing methods.
internal static class ReaperWispMaterial
{
    private static Effect effect;
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[160];
    internal static void Unload() => effect = null;

    internal static void Impact(Vector2 point, ReaperKind kind, float progress, float angle)
    {
        effect ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ReaperWispFlare", AssetRequestMode.ImmediateLoad).Value;
        effect.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        effect.Parameters["Pigment"].SetValue(ReaperRules.Color(kind).ToVector3());
        effect.Parameters["Highlight"].SetValue(ReaperRules.Edge(kind).ToVector3());
        effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
        effect.Parameters["Seed"].SetValue(0f);
        effect.Parameters["Mode"].SetValue(2f);
        effect.Parameters["Progress"].SetValue(progress);
        effect.Parameters["Opacity"].SetValue(MathF.Pow(1 - progress, 1.3f));
        Vector2 center = point - Main.screenPosition;
        Vector2 x = angle.ToRotationVector2() * 62, y = x.RotatedBy(MathHelper.PiOver2) * .85f;
        vertices[0] = new(new Vector3(center - x - y, 0), Color.White, new(0, 0));
        vertices[1] = new(new Vector3(center + x - y, 0), Color.White, new(1, 0));
        vertices[2] = new(new Vector3(center - x + y, 0), Color.White, new(0, 1));
        vertices[3] = new(new Vector3(center + x + y, 0), Color.White, new(1, 1));
        Main.spriteBatch.End();
        try {
            var device = Main.spriteBatch.GraphicsDevice;
            device.BlendState = BlendState.Additive;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            // Called inside the additive particle group: restore that group's state.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void Draw(ReaperWisp wisp)
    {
        Projectile q = wisp.Projectile;
        effect ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ReaperWispFlare", AssetRequestMode.ImmediateLoad).Value;
        effect.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        effect.Parameters["Pigment"].SetValue(ReaperRules.Color(wisp.Kind).ToVector3());
        effect.Parameters["Highlight"].SetValue(ReaperRules.Edge(wisp.Kind).ToVector3());
        effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
        effect.Parameters["Seed"].SetValue(q.identity * .73f);
        effect.Parameters["Opacity"].SetValue(q.Opacity);
        var device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try {
            device.BlendState = BlendState.Additive;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["Mode"].SetValue(0f);
            int count = BuildTrail(q);
            if (count >= 4) {
                effect.CurrentTechnique.Passes[0].Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count - 2);
            }
            effect.Parameters["Mode"].SetValue(1f);
            Vector2 axis = q.rotation.ToRotationVector2(), normal = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 center = q.Center - axis * 7 - Main.screenPosition;
            float pulse = 1 + MathF.Sin(wisp.Age * .48f) * .07f;
            Vector2 x = axis * (32 * pulse), y = normal * (22 * pulse);
            vertices[0] = new(new Vector3(center - x - y, 0), Color.White, new(0, 0));
            vertices[1] = new(new Vector3(center + x - y, 0), Color.White, new(1, 0));
            vertices[2] = new(new Vector3(center - x + y, 0), Color.White, new(0, 1));
            vertices[3] = new(new Vector3(center + x + y, 0), Color.White, new(1, 1));
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
        using (new ChargedOperatorVfx.Pass(true)) {
            // Wide low-opacity mantle around the small hot center; the body comes from the shader.
            ChargedOperatorVfx.Sprite("OrbitSmear", q.Center, new Vector2(66, 42),
                ReaperRules.Color(wisp.Kind) * (.3f * q.Opacity), q.rotation);
            ChargedOperatorVfx.Sprite("StarCore", q.Center, new Vector2(25, 15),
                ReaperRules.Edge(wisp.Kind) * (.85f * q.Opacity), q.rotation);
        }
    }
    private static int BuildTrail(Projectile q)
    {
        int count = 0;
        float length = 0;
        Vector2 previous = q.Center;
        int samples = Math.Min(q.oldPos.Length, 24);
        for (int i = 0; i < samples; i++) {
            if (q.oldPos[i] == Vector2.Zero) break;
            Vector2 point = q.oldPos[i] + q.Size * .5f;
            Vector2 delta = previous - point;
            if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() > 96 * 96) break;
            length += delta.Length();
            if (length > 340) break;
            for (int sub = 0; sub < 2; sub++) {
                float t = (i + sub * .5f) / samples;
                Vector2 at = Vector2.Lerp(previous, point, sub * .5f);
                Vector2 normal = delta.SafeNormalize(q.rotation.ToRotationVector2()).RotatedBy(MathHelper.PiOver2);
                float width = 12 * MathF.Pow(1 - t, .75f) * (.8f + .2f * MathF.Sin(t * MathHelper.Pi));
                vertices[count++] = new(new Vector3(at + normal * width - Main.screenPosition, 0), Color.White, new(t, 0));
                vertices[count++] = new(new Vector3(at - normal * width - Main.screenPosition, 0), Color.White, new(t, 1));
            }
            previous = point;
        }
        return count;
    }
}
