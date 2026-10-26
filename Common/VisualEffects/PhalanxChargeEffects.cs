using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

internal static class PhalanxChargeEffects
{
    internal static void Tick(PhalanxFocus focus)
    {
        Projectile p = focus.Projectile;
        int tier = (int)p.ai[0];
        float progress = focus.ChargeProgress;
        Vector2 tip = focus.FocusPosition(), forward = new(0, -Main.player[p.owner].gravDir);
        Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
        float timer = progress * 90;
        // Mirrors NL_SHPC_EXWeapon.SpawnBurstEffect's ramp and overlapping pulses.
        float pulseA = .5f + .5f * MathF.Sin(timer * .18f + progress * 4.4f);
        float pulseB = .5f + .5f * MathF.Cos(timer * .41f - progress * 8.2f);
        float pulseC = .5f + .5f * MathF.Sin(timer * .09f + pulseB * MathHelper.TwoPi);
        float energy = progress * progress * (1.1f + pulseA * .45f + pulseB * .3f);
        if (p.owner == Main.myPlayer)
        {
            int type = ModContent.ProjectileType<PhalanxChargeSoul>();
            int active = Main.player[p.owner].ownedProjectileCounts[type];
            float cycle = .5f + .5f * MathF.Sin(timer * .28f + progress * 7);
            int attempts = 1 + (progress > .55f ? 1 : 0) + (progress > .83f ? 1 : 0);
            int cap = 16 + (int)(progress * 14);
            for (int i = 0; i < attempts && active < cap; i++)
            {
                if (Main.rand.NextFloat() > .24f + progress * .5f + cycle * .15f - i * .12f) continue;
                float angle = timer * .19f + i * MathHelper.TwoPi / attempts + p.identity * .37f;
                float radial = 60 + 55 * (.5f + .5f * MathF.Sin(angle * 1.37f + progress * 5.8f));
                float axial = 28 + 42 * (.5f + .5f * MathF.Cos(angle * .91f - progress * 3.4f));
                Vector2 start = Main.player[p.owner].Center + right * (MathF.Sin(angle) * radial)
                    + forward * (MathF.Cos(angle * 1.21f + .6f) * axial - 18) + Main.rand.NextVector2Circular(10, 10);
                Vector2 inward = (tip - start).SafeNormalize(forward);
                Vector2 velocity = inward * MathHelper.Lerp(4.5f, 11.5f, progress)
                    + inward.RotatedBy(MathHelper.PiOver2) * (Main.rand.NextBool() ? 1 : -1)
                    * MathHelper.Lerp(1.8f, 4.5f, progress) * Main.rand.NextFloat(.75f, 1.15f);
                Projectile.NewProjectile(p.GetSource_FromThis(), start, velocity, type, 0, 0, p.owner,
                    p.identity, tier, focus.ReleaseSequence);
                active++;
            }
        }
        if (ParticleManager.activeParticles.Count >= 1300) return;
        Color color = PhalanxVisuals.Palette(tier);
        int lanes = 3 + (progress > .62f ? 1 : 0);
        for (int lane = 0; lane < lanes; lane++)
        {
            float angle = timer * (.14f + lane * .026f) + lane * MathHelper.TwoPi / lanes + p.identity * .19f;
            float radius = MathHelper.Lerp(24, 110, .25f + .75f * (.5f + .5f * MathF.Sin(angle * .93f + pulseA * 2.1f)));
            Vector2 start = tip + right * (MathF.Sin(angle * 1.3f) * radius)
                + forward * (-(38 + radius * .58f) + MathF.Cos(angle * .6f) * 16);
            Vector2 inward = (tip - start).SafeNormalize(forward);
            Vector2 velocity = inward * MathHelper.Lerp(2.6f, 7.4f, progress)
                + inward.RotatedBy(MathHelper.PiOver2 * (lane % 2 == 0 ? 1 : -1))
                * (6 + radius * .035f) * (.55f + .45f * pulseB) + forward * (1.5f + 2.5f * pulseC);
            new T_PhalanxChargeMote(start, velocity, color, 24, new Vector2(10, 18) * energy, false).Spawn();
            if (lane == 0) MaterialMote(start, velocity * .35f, tier, 20);
        }
        int sparks = 2 + (int)(progress * 3 + pulseA * 2);
        for (int i = 0; i < sparks; i++)
        {
            float angle = timer * .22f + i * MathHelper.TwoPi / sparks + pulseB * .7f;
            float radius = MathHelper.Lerp(20, 72, .3f + .7f * (.5f + .5f * MathF.Cos(angle * 1.17f - progress * 6.1f)));
            Vector2 start = tip + right * (MathF.Cos(angle) * radius) - forward * (18 + radius * .42f);
            Vector2 inward = (tip - start).SafeNormalize(forward);
            OperatorFlightVfx.Spark(start, inward * Main.rand.NextFloat(6.5f, 12) * (.65f + .55f * progress),
                Color.Lerp(color, Color.White, .4f), 16 + progress * 8, 12, 2);
        }
        if ((Main.GameUpdateCount & 1) == 0)
        {
            float angle = -timer * .16f + pulseC * 1.7f;
            Vector2 start = tip + right * MathF.Cos(angle) * 36 - forward * 6;
            new T_PhalanxChargeMark(start, (tip - start).SafeNormalize(forward) * 1.8f,
                Color.Lerp(color, Color.White, .4f), 24, new Vector2(25, 11) * (.85f + progress * .4f)).Spawn();
        }
        new T_PhalanxChargeMote(tip + Main.rand.NextVector2Circular(6, 6),
            Main.rand.NextVector2Circular(.7f, .7f), Color.Lerp(color, Color.White, .7f),
            18, new Vector2(14) * (1 + progress), false).Spawn();
    }
    internal static void MaterialMote(Vector2 point, Vector2 velocity, int tier, int life)
    {
        if (Main.dedServ) return;
        int type = tier == 0 ? DustID.Sand : tier == 1 ? DustID.SandstormInABottle : DustID.GemAmethyst;
        Dust dust = Dust.NewDustPerfect(point, type, velocity, 80, PhalanxVisuals.Palette(tier), tier == 1 ? 1.1f : .8f);
        dust.noGravity = true;
    }
}
