using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Leonhardt;

internal static class LeonhardtVisuals
{
    internal static void DrawCharge(Vector2 muzzle, float charge, float time)
        => SpreadArtsVfx.Charge(muzzle, charge, time, SpreadArt.Rupture);

    internal static void DrawCore(Projectile projectile, float charge)
        => SpreadArtsVfx.Flight(projectile, SpreadArt.Rupture, .85f + .25f * charge);

    internal static void DrawDetonation(Vector2 worldCenter, float diameter, float progress)
        => SpreadArtsVfx.Burst(worldCenter, diameter, progress, SpreadArt.Rupture);

    internal static void ChargeParticles(Vector2 muzzle, float charge)
        => SpreadArtsVfx.Gather(muzzle, charge, SpreadArt.Rupture);

    internal static void FireParticles(Vector2 muzzle, Vector2 direction)
        => SpreadArtsVfx.Release(muzzle, direction, SpreadArt.Rupture);

    internal static void DetonationParticles(Vector2 center, float charge)
        => SpreadArtsVfx.Debris(center, SpreadArt.Rupture, .7f + .4f * charge);
}
