using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

// Archived 2026-10-28: kept for reference. ReaperWisp no longer calls this
// previous flight/impact effect; the active route is Phoenix/ReaperSwarmInstance.
internal static class ReaperWispVisuals
{
    private static bool CanSpawn(Vector2 point) => ReaperVisuals.Visible(point) && ParticleManager.activeParticles.Count < 1100;
    internal static void Flight(ReaperWisp wisp)
    {
        Projectile q = wisp.Projectile;
        if (!CanSpawn(q.Center)) return;
        Lighting.AddLight(q.Center, ReaperRules.Color(wisp.Kind).ToVector3() * (.38f * q.Opacity));
        Vector2 axis = q.velocity.SafeNormalize(Vector2.UnitX), side = axis.RotatedBy(MathHelper.PiOver2);
        if (wisp.Age == 0) {
            for (int i = 0; i < 5; i++)
                new T_Reaper_WispSpark(q.Center, q.velocity.RotatedBy(Main.rand.NextFloat(-.6f, .6f)) * Main.rand.NextFloat(.2f, .7f), wisp.Kind, 14, 3).Spawn();
        }
        if (wisp.Age % 3 == 0) {
            for (int i = 0; i < 2; i++) {
                Vector2 at = q.Center + side * Main.rand.NextFloat(-7, 7) - axis * Main.rand.NextFloat(0, 12);
                Vector2 speed = -q.velocity * .12f + side * Main.rand.NextFloat(-2.6f, 2.6f);
                new T_Reaper_WispSpark(at, speed, wisp.Kind, Main.rand.Next(13, 23), Main.rand.NextFloat(2.2f, 3.6f)).Spawn();
            }
        }
        if (wisp.Age % 8 == 0) {
            new T_Reaper_WispVapor(q.Center - axis * 12, -q.velocity * .08f, wisp.Kind).Spawn();
            Dust dust = Dust.NewDustPerfect(q.Center, DustID.GoldFlame, -q.velocity * .08f, 100, ReaperRules.Edge(wisp.Kind), .8f);
            dust.noGravity = true; dust.noLight = true;
        }
    }
    internal static void Burst(Vector2 point, Vector2 velocity, ReaperKind kind, bool impact)
    {
        if (!CanSpawn(point)) return;
        if (impact) new T_Reaper_WispBurst(point, kind, velocity.ToRotation()).Spawn();
        int count = impact ? 12 : 4;
        for (int i = 0; i < count; i++) {
            Vector2 ray = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.18f, .18f)).ToRotationVector2();
            new T_Reaper_WispSpark(point, ray * Main.rand.NextFloat(impact ? 2.5f : 1, impact ? 7 : 3),
                kind, Main.rand.Next(15, 25), impact ? 3.5f : 2.5f).Spawn();
        }
        new T_Reaper_WispVapor(point, velocity * .05f, kind).Spawn();
    }
}
