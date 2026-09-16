using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

internal static class ScatterVisuals
{
    internal static Color ColorFor(int tier) => tier switch
    { 0 => new Color(255, 186, 83), 1 => new Color(194, 215, 247), _ => new Color(62, 217, 255) };
    internal const float ExecutorSmokeScale = .4f;
    internal static void ExecutorMuzzle(Vector2 center, Vector2 direction)
    {
        if (Main.dedServ) return;
        // BrassBeastHeavySmoke's +/-45 degree Torch/Smoke plume, reduced to 40% linear scale.
        // Emit once per shot, without copying its huge damaging projectile or per-update emissions.
        for (int i = 0; i < 64; i++)
        {
            Vector2 velocity = direction.RotatedByRandom(MathHelper.PiOver4)
                * Main.rand.NextFloat(10f, 40f) * ExecutorSmokeScale;
            int type = Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke;
            Dust dust = Dust.NewDustPerfect(center, type, velocity, 100, default,
                Main.rand.NextFloat(3.5f, 6f) * Main.rand.NextFloat(.8f, 1.35f) * ExecutorSmokeScale);
            dust.noGravity = true;
        }
    }
    internal static void WaterTrail(Projectile projectile)
    {
        if (Main.dedServ) return;
        // AquaBlast / Leviatitan reference: aquamarine mist and short-lived vanilla bubbles.
        if (Main.rand.NextBool(3))
        {
            Gore bubble = Gore.NewGorePerfect(projectile.GetSource_FromAI(), projectile.Center,
                projectile.velocity * .15f + Main.rand.NextVector2Circular(1, 1), Main.rand.NextBool(3) ? 412 : 411);
            bubble.timeLeft = Main.rand.Next(6, 13); bubble.scale = Main.rand.NextFloat(.35f, .55f);
        }
        Dust mist = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(5, 5),
            Main.rand.NextBool(5) ? 267 : 278, -projectile.velocity * Main.rand.NextFloat(.05f, .3f),
            0, Main.rand.NextBool(5) ? Color.Aqua : Color.Aquamarine, Main.rand.NextFloat(.3f, .5f));
        mist.noGravity = true;
    }
    internal static void Spray(Vector2 center, Vector2 direction, int tier, int count, float force)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = direction.RotatedByRandom(.7f) * Main.rand.NextFloat(1, force);
            Dust d = Dust.NewDustPerfect(center, tier == 2 ? DustID.Water : DustID.TintableDustLighted,
                velocity, tier == 2 ? 80 : 0, ColorFor(tier), Main.rand.NextFloat(.8f, 1.7f));
            d.noGravity = tier != 2;
        }
    }
}
