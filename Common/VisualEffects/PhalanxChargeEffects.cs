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
        float phase = Main.GameUpdateCount * .07f + p.identity * .37f;
        // Evenly spaced emitters on one ellipse, anchored to the staff tip.
        // No random kick, opposing momentum, or pursuit of a moving player.
        if (p.owner == Main.myPlayer && Main.GameUpdateCount % 6 == 0)
        {
            int type = ModContent.ProjectileType<PhalanxChargeSoul>(), active = 0;
            foreach (Projectile soul in Main.ActiveProjectiles)
                if (soul.owner == p.owner && soul.type == type && (int)soul.ai[0] == p.identity) active++;
            if (active < 18)
            {
                float angle = phase + active * 2.399963f;
                Vector2 offset = right * (MathF.Cos(angle) * 100) + forward * (MathF.Sin(angle) * 72);
                Projectile.NewProjectile(p.GetSource_FromThis(), tip + offset, Vector2.Zero, type, 0, 0, p.owner,
                    p.identity, tier, focus.ReleaseSequence);
            }
        }
        if (ParticleManager.activeParticles.Count >= 1300 || Main.GameUpdateCount % 2 != 0) return;
        Color color = PhalanxVisuals.Palette(tier);
        for (int lane = 0; lane < 3; lane++)
        {
            float angle = phase + lane * MathHelper.TwoPi / 3;
            float radius = 38 + 32 * (1 - progress);
            Vector2 offset = right * (MathF.Cos(angle) * radius) + forward * (MathF.Sin(angle) * radius * .72f);
            Vector2 tangent = right * -MathF.Sin(angle) + forward * (MathF.Cos(angle) * .72f);
            new T_PhalanxChargeMote(tip + offset, tangent * 1.5f, color, 24,
                new Vector2(8, 14) * (.5f + progress), false, focus.FocusPosition).Spawn();
        }
        new T_PhalanxChargeMote(tip, Vector2.Zero, Color.Lerp(color, Color.White, .7f),
            18, new Vector2(14) * (1 + progress), false, focus.FocusPosition).Spawn();
    }
    internal static void MaterialMote(Vector2 point, Vector2 velocity, int tier, int life)
    {
        if (Main.dedServ) return;
        int type = tier == 0 ? DustID.Sand : tier == 1 ? DustID.SandstormInABottle : DustID.GemAmethyst;
        Dust dust = Dust.NewDustPerfect(point, type, velocity, 80, PhalanxVisuals.Palette(tier), tier == 1 ? 1.1f : .8f);
        dust.noGravity = true;
    }
}
