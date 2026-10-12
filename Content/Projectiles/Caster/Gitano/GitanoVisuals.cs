using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Gitano;

internal static class GitanoVisuals
{
    internal static void DrawCharge(Vector2 muzzle, float charge, float time)
        => SpreadArtsVfx.Charge(muzzle, charge, time, SpreadArt.Crystal);

    internal static void DrawCrystal(Projectile projectile)
        => SpreadArtsVfx.Flight(projectile, SpreadArt.Crystal);

    internal static void DrawBurst(Vector2 worldCenter, bool fate, float progress)
        => SpreadArtsVfx.Burst(worldCenter, fate ? 290f : 180f, progress, SpreadArt.Crystal);

    internal static void ChargeParticles(Vector2 muzzle, float charge)
        => SpreadArtsVfx.Gather(muzzle, charge, SpreadArt.Crystal);

    internal static void FireParticles(Vector2 muzzle, Vector2 direction, bool fate)
        => SpreadArtsVfx.Release(muzzle, direction, SpreadArt.Crystal);

    internal static void FlightParticles(Vector2 center, Vector2 velocity)
        => SpreadArtsVfx.Wake(center, velocity, SpreadArt.Crystal);

    internal static void BurstParticles(Vector2 center, bool fate)
        => SpreadArtsVfx.Debris(center, SpreadArt.Crystal, fate ? 1.2f : 1f);

    internal static void TargetParticles(Vector2 target)
        => OperatorFlightVfx.Ring(target, new Color(229, 204, 255), 32f, 12);
}
