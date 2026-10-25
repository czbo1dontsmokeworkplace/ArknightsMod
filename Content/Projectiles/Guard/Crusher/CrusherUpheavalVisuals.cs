using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherUpheavalVisuals
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[4];
    internal static void Unload() => material = null;
    internal static void Launch(Projectile q, CrusherKind kind)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = .65f, Pitch = -.55f, MaxInstances = 3 }, q.Center);
        for (int i = 0; i < 12 && ParticleManager.activeParticles.Count < 1100; i++) {
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-12, -4));
            new T_Crusher_Fragment(q.Center + new Vector2(Main.rand.NextFloat(-32, 32), 0), velocity,
                kind, 18, Main.rand.NextFloat(8, 16), false).Spawn();
        }
    }
    internal static void Flight(Projectile q, CrusherKind kind)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        Lighting.AddLight(q.Center, CrusherRules.Edge(kind).ToVector3() * 1.25f);
        Vector2 axis = q.velocity.SafeNormalize(-Vector2.UnitY), side = axis.RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < 3 && ParticleManager.activeParticles.Count < 1100; i++) {
            new T_Crusher_Fragment(q.Center + side * Main.rand.NextFloat(-22, 22),
                -axis * Main.rand.NextFloat(2, 5) + side * Main.rand.NextFloat(-2, 2),
                kind, 14, Main.rand.NextFloat(6, 12), false).Spawn();
        }
        if (kind == CrusherKind.Ulpianus) {
            Dust d = Dust.NewDustPerfect(q.Center + side * Main.rand.NextFloat(-24, 24), DustID.Water, -axis * 5, 30, default, 1.4f);
            d.noGravity = true;
        }
    }
    internal static void Draw(Projectile q, CrusherKind kind, Vector2[] history, int samples)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        CrusherWaveMaterial.DrawUpheavalTrail(q, history, samples, kind);
        DrawBody(q, kind);
        Vector2 axis = q.velocity.SafeNormalize(-Vector2.UnitY);
        if (kind == CrusherKind.Ulpianus) {
            // The same original anchor sprite becomes a heavy rising crown.
            float reach = CrusherRules.Reach(kind) * 1.65f;
            for (int i = Math.Min(samples - 1, 15); i >= 3; i -= 4) {
                Vector2 at = history[i];
                CrusherVisuals.DrawBlade(at - axis * reach * .5f, at + axis * reach * .5f, kind, .22f * (1 - i / 18f) * q.Opacity, false);
            }
            CrusherVisuals.DrawBlade(q.Center - axis * reach * .5f, q.Center + axis * reach * .5f, kind, q.Opacity, true);
        }
        using (new ChargedOperatorVfx.Pass(true)) {
            float rotation = axis.ToRotation();
            Color edge = CrusherRules.Edge(kind) * q.Opacity;
            if (kind == CrusherKind.Hoederer || kind == CrusherKind.Ulpianus)
                ChargedOperatorVfx.Sprite("ForwardFlame", q.Center - axis * 35, new Vector2(170, 52 + (int)kind * 8), edge * .8f, rotation);
            ChargedOperatorVfx.Sprite("StarCore", q.Center + axis * 22, new Vector2(kind == CrusherKind.Quartz ? 36 : 54, 25), Color.White * (.9f * q.Opacity), rotation);
            if (kind == CrusherKind.WindChimes) {
                for (int i = -1; i <= 1; i += 2)
                    ChargedOperatorVfx.Sprite("LinearSpark", q.Center + axis.RotatedBy(i * .5f) * -35,
                        new Vector2(132, 16), edge * .8f, rotation + i * .16f);
            }
        }
    }
    private static void DrawBody(Projectile q, CrusherKind kind)
    {
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CrusherUpheaval", AssetRequestMode.ImmediateLoad).Value;
        Vector2 axis = q.velocity.SafeNormalize(-Vector2.UnitY);
        Vector2 x = axis * (kind == CrusherKind.Ulpianus ? 120 : 92), y = axis.RotatedBy(MathHelper.PiOver2) * (30 + (int)kind * 7);
        Vector2 center = q.Center - Main.screenPosition;
        quad[0] = new(new Vector3(center - x - y, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(center + x - y, 0), Color.White, new(1, 0));
        quad[2] = new(new Vector3(center - x + y, 0), Color.White, new(0, 1));
        quad[3] = new(new Vector3(center + x + y, 0), Color.White, new(1, 1));
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["BodyTint"].SetValue(CrusherRules.Tint(kind).ToVector3());
        material.Parameters["EdgeTint"].SetValue(CrusherRules.Edge(kind).ToVector3());
        material.Parameters["Phase"].SetValue(Main.GlobalTimeWrappedHourly);
        material.Parameters["Style"].SetValue((float)kind); material.Parameters["Opacity"].SetValue(q.Opacity);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try {
            device.BlendState = BlendState.Additive; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes["BodyPass"].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, quad, 0, 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
