using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Gitano;

internal static class GitanoVisuals
{
    internal static void DrawCharge(Vector2 muzzle, float charge, float time, Vector2 direction)
        => SpreadArtsVfx.Charge(muzzle, charge, time, SpreadArt.Crystal, direction);

    internal static void DrawCrystal(Projectile projectile)
        => SpreadArtsVfx.Flight(projectile, SpreadArt.Crystal);

    internal static void DrawBurst(Vector2 worldCenter, bool fate, float progress)
    {
        SpreadArtsVfx.Burst(worldCenter, fate ? 290f : 160f, progress, SpreadArt.Crystal);
        if (!fate || !SpreadVfxBudget.Visible(worldCenter, 940f)) return;
        // Fate really checks a 900px radius: show a sparse boundary, not a misleading
        // 145px damage disk or an opaque full-screen explosion.
        for (int i = 0; i < 48; i++)
        {
            float a = i * MathHelper.TwoPi / 48f;
            SpreadVfxDetail.Segment(worldCenter + a.ToRotationVector2() * 900f,
                worldCenter + (a + .065f).ToRotationVector2() * 900f,
                OperatorFlightVfx.Add(new Color(173, 128, 227), (1f - progress) * .42f), 1.5f);
        }
    }

    internal static void ChargeParticles(Vector2 muzzle, float charge)
        => SpreadArtsVfx.Gather(muzzle, charge, SpreadArt.Crystal);

    internal static void FireParticles(Vector2 muzzle, Vector2 direction, bool fate)
        => SpreadArtsVfx.Release(muzzle, direction, SpreadArt.Crystal);

    internal static void FlightParticles(Vector2 center, Vector2 velocity, int phase)
        => SpreadArtsVfx.Wake(center, velocity, SpreadArt.Crystal, phase);

    internal static void BurstParticles(Vector2 center, bool fate)
        => SpreadArtsVfx.Debris(center, SpreadArt.Crystal, fate ? 145f : 80f);

    internal static void TargetParticles(Vector2 target)
        => OperatorFlightVfx.Ring(target, new Color(229, 204, 255), 32f, 12);
}
