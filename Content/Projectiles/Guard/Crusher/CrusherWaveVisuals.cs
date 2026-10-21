using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherWaveVisuals
{
    internal static void DrawWave(Projectile projectile, CrusherKind kind, float progress)
    {
        if (!CrusherVisuals.Visible(projectile.Center)) return;
        Texture2D texture = TextureAssets.Projectile[ProjectileID.TerraBlade2Shot].Value;
        Rectangle body = texture.Frame(1, 4), core = texture.Frame(1, 4, 0, 3);
        Vector2 origin = body.Size() * .5f, at = projectile.Center - Main.screenPosition;
        float opacity = projectile.Opacity, sign = projectile.ai[1] < 0 ? -1 : 1;
        float opening = sign * .38f * (1 - progress), scale = projectile.scale;
        Color tint = CrusherRules.Tint(kind), edge = CrusherRules.Edge(kind);
        SpriteEffects first = sign > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
        SpriteEffects second = first ^ SpriteEffects.FlipVertically;
        void Draw(Rectangle frame, Color color, float angle, float size, SpriteEffects flip) =>
            Main.EntitySpriteDraw(texture, at, frame, color * opacity, projectile.rotation + angle, origin, scale * size, flip);
        // Native Terra Blade body and closing split crescent remain recognizable.
        Draw(body, tint * .48f, -opening, 1, first); Draw(body, tint * .48f, opening, 1, second);
        Draw(body, tint * .65f, opening * .55f, .98f, first);
        Draw(core, edge * .85f, opening * .2f, .98f, first);
        Draw(core, Color.White * .65f, -opening * .12f, .74f, second);
        CrusherWaveMaterial.DrawRim(projectile, kind);
        using (new ChargedOperatorVfx.Pass(true)) {
            float pulse = .85f + .15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18);
            for (int i = -2; i <= 2; i++) {
                float angle = projectile.rotation + i * .29f;
                Vector2 point = projectile.Center + angle.ToRotationVector2() * 89 * scale;
                float size = (i == 0 ? 20 : 12) * scale;
                ChargedOperatorVfx.Sprite("StarCore", point, new Vector2(size, size * .42f) * pulse,
                    edge * (.6f * opacity), angle + MathHelper.PiOver4);
            }
        }
    }
    internal static void WaveMotes(Projectile projectile, CrusherKind kind)
    {
        if (ParticleManager.activeParticles.Count > 1100) return;
        for (int i = 0; i < 2; i++) {
            float angle = projectile.rotation + Main.rand.NextFloat(-.7f, .7f);
            Vector2 ray = angle.ToRotationVector2(), at = projectile.Center + ray * 88 * projectile.scale;
            new T_Crusher_Fragment(at, -projectile.velocity * .05f + ray.RotatedBy(MathHelper.PiOver2) * 1.5f,
                kind, 16, 5, false).Spawn();
        }
    }
    internal static void Emission(Vector2 at, Vector2 velocity, CrusherKind kind)
    {
        if (!CrusherVisuals.Visible(at)) return;
        ChargedOperatorVfx.Sparkle(at, velocity.SafeNormalize(Vector2.UnitX) * 1.2f,
            CrusherRules.Edge(kind), velocity.ToRotation(), .65f);
        if (ParticleManager.activeParticles.Count > 1100) return;
        for (int i = 0; i < 4; i++)
            new T_Crusher_Fragment(at, Main.rand.NextVector2Circular(2.2f, 2.2f), kind, 12, 5, false).Spawn();
    }
    internal static void CoreMote(Projectile projectile, CrusherKind kind)
    {
        if (ParticleManager.activeParticles.Count > 1100) return;
        Vector2 side = projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        new T_Crusher_Fragment(projectile.Center + side * Main.rand.NextFloat(-5, 5),
            -projectile.velocity * .05f, kind, 12, 4, false).Spawn();
    }
    internal static void Impact(Vector2 point, CrusherKind kind, float size)
    {
        if (!CrusherVisuals.Visible(point)) return;
        ChargedOperatorVfx.Sparkle(point, Vector2.Zero, CrusherRules.Edge(kind), Main.rand.NextFloat(MathHelper.TwoPi), size);
        for (int i = 0; i < 6 && ParticleManager.activeParticles.Count < 1100; i++) {
            Vector2 ray = (i * MathHelper.TwoPi / 6).ToRotationVector2();
            new T_Crusher_Fragment(point, ray * (2 + size * 3), kind, 15, 6 * size + 3, false).Spawn();
        }
    }
    internal static void Dissipate(Vector2 point, float rotation, CrusherKind kind, float scale)
    {
        if (!CrusherVisuals.Visible(point)) return;
        for (int i = 0; i < 12 && ParticleManager.activeParticles.Count < 1100; i++) {
            Vector2 ray = (rotation + MathHelper.Lerp(-.8f, .8f, i / 11f)).ToRotationVector2();
            new T_Crusher_Fragment(point + ray * 85 * scale, ray * 1.4f, kind, 22, 7, false).Spawn();
        }
    }
}
