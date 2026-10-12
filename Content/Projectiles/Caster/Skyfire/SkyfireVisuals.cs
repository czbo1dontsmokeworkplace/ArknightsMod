using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Skyfire;

internal static class SkyfireVisuals
{
    internal static void DrawCharge(Vector2 muzzle, float charge, float time)
        => SpreadArtsVfx.Charge(muzzle, charge, time, SpreadArt.Meteor);

    internal static void DrawMeteor(Projectile projectile, bool skyfall)
        => SpreadArtsVfx.Flight(projectile, SpreadArt.Meteor, skyfall ? 1.2f : 1f);

    internal static void DrawImpact(Vector2 worldCenter, float diameter, float progress)
        => SpreadArtsVfx.Burst(worldCenter, diameter, progress, SpreadArt.Meteor);

    internal static void ChargeParticles(Vector2 muzzle, float charge)
        => SpreadArtsVfx.Gather(muzzle, charge, SpreadArt.Meteor);

    internal static void FireParticles(Vector2 muzzle, Vector2 direction)
        => SpreadArtsVfx.Release(muzzle, direction, SpreadArt.Meteor);

    internal static void FlightParticles(Vector2 center, Vector2 velocity)
        => SpreadArtsVfx.Wake(center, velocity, SpreadArt.Meteor);

    internal static void ImpactParticles(Vector2 center, bool skyfall)
        => SpreadArtsVfx.Debris(center, SpreadArt.Meteor, skyfall ? 1.2f : 1f);
}
