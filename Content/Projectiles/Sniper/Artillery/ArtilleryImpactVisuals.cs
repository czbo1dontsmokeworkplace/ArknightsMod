using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

internal static class ArtilleryBlast
{
    internal const int Diameter = 280;
    internal const float Radius = Diameter * .5f;
}

/// <summary>Native smoke Gore (61–63), with separate fire/debris Dust. No damaging children.</summary>
internal static class ArtilleryImpactVisuals
{
    internal const int SmokeCount = 36;
    internal const float ParticleIntensity = 1.3f;
    internal static int BoostCount(int count) => (int)MathF.Round(count * ParticleIntensity, MidpointRounding.AwayFromZero);

    internal static void Spawn(Projectile explosion)
    {
        if (Main.dedServ || !new Rectangle((int)Main.screenPosition.X - 480,
            (int)Main.screenPosition.Y - 480, Main.screenWidth + 960, Main.screenHeight + 960)
            .Contains(explosion.Center.ToPoint())) return;

        Vector2 center = explosion.Center;
        // Visual volume stays consistent even when a skill enlarges the damage circle.
        float radius = ArtilleryBlast.Radius;
        float phase = Main.rand.NextFloat(MathHelper.TwoPi);
        var source = explosion.GetSource_FromThis();
        for (int i = 0; i < SmokeCount; i++)
        {
            // Three staggered shells fill the footprint instead of a hollow ring of smoke.
            int layer = i / 12;
            Vector2 direction = (phase + (i % 12) * MathHelper.TwoPi / 12f + layer * .19f)
                .ToRotationVector2();
            float spread = layer switch { 0 => .16f, 1 => .42f, _ => .68f };
            Vector2 position = center + direction * radius * spread
                + Main.rand.NextVector2Circular(9f, 9f);
            Vector2 velocity = direction * Main.rand.NextFloat(.8f, 2.5f) - Vector2.UnitY * .65f;
            int index = Gore.NewGore(source, position - new Vector2(24f), velocity,
                Main.rand.Next(61, 64), Main.rand.NextFloat(1.05f, 1.55f));
            if ((uint)index < (uint)Main.gore.Length && Main.gore[index].active)
            {
                Gore smoke = Main.gore[index];
                smoke.velocity = velocity;
                smoke.alpha = Main.rand.Next(10, 45);
                ArtillerySmokeLifetime.Track(smoke);
            }
        }

        // Fine bright embers and heavier debris are Dust, distinct from the smoke Gore above.
        int fireCount = BoostCount(16);
        for (int i = 0; i < BoostCount(24); i++)
        {
            Vector2 direction = Main.rand.NextVector2Unit();
            Dust dust = Dust.NewDustPerfect(center + direction * Main.rand.NextFloat(4f, radius * .52f),
                i < fireCount ? (i % 3 == 0 ? DustID.GoldFlame : DustID.Torch) : DustID.Sand,
                direction * Main.rand.NextFloat(3f, 8f) * ParticleIntensity, i < fireCount ? 30 : 85,
                i < fireCount ? new Color(255, 197, 98) : new Color(175, 151, 115),
                Main.rand.NextFloat(.9f, 1.5f));
            dust.noGravity = i < fireCount;
        }
    }
}
