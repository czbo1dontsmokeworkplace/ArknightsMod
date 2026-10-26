using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class UlpianusExplosionVisuals
{
    private const CrusherKind Kind = CrusherKind.Ulpianus;
    internal static void Light(Projectile q)
    {
        if (Main.dedServ) return;
        float fade = Math.Max(0, 1 - q.ai[0] / UlpianusAnchorExplosion.Lifetime);
        Lighting.AddLight(q.Center, CrusherRules.Edge(Kind).ToVector3() * (1.4f * fade * fade));
    }
    internal static void Burst(Projectile q)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .8f, Pitch = -.5f, MaxInstances = 3 }, q.Center);
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .7f, Pitch = -.4f, MaxInstances = 3 }, q.Center);
        for (int i = 0; i < 28 && ParticleManager.activeParticles.Count < 1200; i++) {
            Vector2 axis = (i * MathHelper.TwoPi / 28 + Main.rand.NextFloat(-.05f, .05f)).ToRotationVector2();
            new T_Crusher_Fragment(q.Center + axis * 8, axis * Main.rand.NextFloat(5, 12),
                Kind, Main.rand.Next(17, 28), Main.rand.NextFloat(9, 16), false).Spawn();
            if (i % 4 == 0)
                ChargedOperatorVfx.Sparkle(q.Center + axis * 12, axis * 2.5f,
                    CrusherRules.Edge(Kind), axis.ToRotation(), 1.05f);
        }
        for (int i = 0; i < 16; i++) {
            Vector2 axis = Main.rand.NextVector2CircularEdge(1, 1);
            Dust dust = Dust.NewDustPerfect(q.Center + axis * 10, DustID.TintableDustLighted,
                axis * Main.rand.NextFloat(3, 8), 30, i % 3 == 0 ? Color.White : CrusherRules.Edge(Kind), 1.25f);
            dust.noGravity = true;
        }
        if (q.owner == Main.myPlayer)
            Main.instance?.CameraModifiers?.Add(new PunchCameraModifier(q.Center, -Vector2.UnitY, 6, 7, 10, 800, "UlpianusContactBlast"));
    }
    internal static void Draw(Projectile q)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        float t = Math.Clamp(q.ai[0] / UlpianusAnchorExplosion.Lifetime, 0, 1);
        float fade = (1 - t) * (1 - t), flash = MathF.Pow(Math.Max(0, 1 - t * 5), 2);
        // Damage is confined to the 150-square hitbox. This slightly wider rim
        // is a fading, non-damaging pressure wave around the same center.
        CrusherMaterial.DrawAnchorExplosion(q.Center, t);
        using (new ChargedOperatorVfx.Pass(true)) {
            ChargedOperatorVfx.Sprite("StarCore", q.Center, new Vector2(105, 76) * (1 - t * .55f), Color.White * flash, .25f);
            ChargedOperatorVfx.Sprite("ImpactPetals", q.Center, new Vector2(120 + 80 * t), CrusherRules.Tint(Kind) * fade, -t * .5f);
            ChargedOperatorVfx.Sprite("RuptureRing", q.Center, new Vector2(36 + 150 * t), CrusherRules.Edge(Kind) * fade, t * .2f);
            ChargedOperatorVfx.Sprite("LinearSpark", q.Center, new Vector2(152, 17), Color.White * (.85f * flash), .2f);
            ChargedOperatorVfx.Sprite("LinearSpark", q.Center, new Vector2(115, 12), CrusherRules.Edge(Kind) * flash, MathHelper.PiOver2 + .2f);
        }
    }
}
