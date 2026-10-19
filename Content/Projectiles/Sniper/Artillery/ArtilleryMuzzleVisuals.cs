using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

internal static class ArtilleryMuzzleVisuals
{
    internal static void Emit(Vector2 muzzle, Vector2 direction, int tier)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1300
            || !new Rectangle((int)Main.screenPosition.X - 180, (int)Main.screenPosition.Y - 180,
                Main.screenWidth + 360, Main.screenHeight + 360).Contains(muzzle.ToPoint())) return;
        tier = System.Math.Clamp(tier, 0, 2);
        direction = direction.SafeNormalize(-Vector2.UnitY);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        Color outer = tier switch { 0 => new(245, 151, 44), 1 => new(246, 91, 29), _ => new(245, 49, 22) };
        Color core = new(255, 238, 178);
        new T_ArtilleryMuzzleFlame(muzzle, direction, outer, core, 72f + tier * 12f).Spawn();
        new T_ArtilleryMuzzleRing(muzzle, direction, core, 62f + tier * 9f, 12).Spawn();
        new T_ArtilleryMuzzleRing(muzzle - direction * 3f, direction * .65f,
            new Color(172, 151, 120), 46f + tier * 8f, 16).Spawn();
        for (int i = 0; i < 14 + tier * 4; i++)
            new T_Streak_Particle(muzzle + normal * Main.rand.NextFloat(-3f, 3f),
                direction.RotatedByRandom(.38f) * Main.rand.NextFloat(3.5f, 10.5f),
                Main.rand.Next(11, 19), Main.rand.NextFloat(.20f, .38f),
                Color.Lerp(outer, core, Main.rand.NextFloat()), true)
            { Deformation = new Vector2(.19f, Main.rand.NextFloat(1.8f, 3.0f)) }.Spawn();
        for (int i = 0; i < 4; i++)
            new T_ArtilleryMuzzleSmoke(muzzle + normal * Main.rand.NextFloat(-4f, 4f),
                direction * Main.rand.NextFloat(.5f, 1.6f) + normal * Main.rand.NextFloat(-.6f, .6f)
                    - Vector2.UnitY * .25f, Main.rand.NextFloat(16f, 24f), Main.rand.Next(14, 21)).Spawn();
        for (int i = 0; i < 8; i++)
        {
            Dust dust = Dust.NewDustPerfect(muzzle, i % 3 == 0 ? DustID.GoldFlame : DustID.Torch,
                direction.RotatedByRandom(.45f) * Main.rand.NextFloat(3f, 8f), 40, outer,
                Main.rand.NextFloat(.85f, 1.3f));
            dust.noGravity = true;
        }
    }
}
