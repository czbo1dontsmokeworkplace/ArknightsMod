using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Charged;

internal static class ChargedSniperFlightVisuals
{
    private const float SampleSpacing = 6f;
    private const float Wavelength = 192f;

    internal static bool UsesHelix(ChargedOperatorKind kind) => kind is
        ChargedOperatorKind.Ambriel or ChargedOperatorKind.Andreana or ChargedOperatorKind.Firewatch;

    internal static void EmitHelix(Projectile projectile, ChargedOperatorKind kind, float traveled)
    {
        if (!UsesHelix(kind) || !ChargedOperatorVfx.Visible(projectile.Center)
            || ParticleManager.activeParticles.Count >= 900 || traveled < SampleSpacing) return;
        float speed = projectile.velocity.Length();
        if (!float.IsFinite(speed) || speed <= .001f || !float.IsFinite(traveled)) return;
        Vector2 aim = projectile.velocity / speed;
        Vector2 normal = new(-aim.Y, aim.X);
        Color theme = ChargedOperatorRules.ColorFor(kind);
        // Sample completed travel in world pixels, independent of extraUpdates. The two
        // opposite elliptical strands adapt KarasawaShot's paired half-turn dust offsets.
        int first = Math.Max(1, (int)MathF.Floor((traveled - speed) / SampleSpacing) + 1);
        int last = (int)MathF.Floor(traveled / SampleSpacing);
        for (int sample = first; sample <= last && ParticleManager.activeParticles.Count < 900; sample++)
        {
            float endDistance = sample * SampleSpacing;
            float startDistance = endDistance - SampleSpacing;
            float startPhase = startDistance * MathHelper.TwoPi / Wavelength;
            float endPhase = endDistance * MathHelper.TwoPi / Wavelength;
            Vector2 end = projectile.Center - aim * (traveled - endDistance);
            Vector2 start = end - aim * SampleSpacing;
            float startWidth = Math.Clamp(startDistance / 30f, 0, 1);
            float endWidth = Math.Clamp(endDistance / 30f, 0, 1);
            Vector2 startOffset = Offset(aim, normal, startPhase) * startWidth;
            Vector2 endOffset = Offset(aim, normal, endPhase) * endWidth;
            new T_Sniper_Flight_Helix(start + startOffset, end + endOffset,
                start - startOffset, end - endOffset, theme, MathF.Sin(endPhase)).Spawn();
        }
    }

    private static Vector2 Offset(Vector2 aim, Vector2 normal, float phase) =>
        aim * (-MathF.Sin(phase) * 7.5f) + normal * (MathF.Cos(phase) * 15f);
}
