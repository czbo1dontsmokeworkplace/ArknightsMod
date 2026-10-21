using System;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static class MaterialistDeathRules
{
    // One source of truth for the two bosses' timing, density ramp and camera envelope.
    internal const int Duration = EvolutionPresentationRules.DeathDuration;
    internal const int Finale = EvolutionPresentationRules.DeathFinale;
    internal const int Aftermath = EvolutionBloodSpectacleRules.FinaleLife;
    internal const int VisualEnd = EvolutionDeathCameraRules.VisualEnd;
    internal const int ParticleCapacity = EvolutionBloodSpectacleRules.ParticleCapacity;
    internal static float Smooth(float from, float to, float age) => EvolutionCinematicFrame.Smooth(from, to, age);
    internal static bool Burst(int age) => EvolutionPresentationRules.DeathBurst(age);
    internal static int BurstCount(int age, bool reduced) => EvolutionPresentationRules.DeathBurstCount(age, reduced);
    internal static int PacketCount(bool reduced) => EvolutionBloodSpectacleRules.DecorationCount(6, reduced);
    internal static float CameraForce(float age) => age < Finale ? MathHelper.Lerp(1, 5, Math.Clamp(age / 180f, 0, 1)) :
        9 * (1 - Smooth(Finale, VisualEnd, age));
    internal static Vector2 FaultPoint(Vector2 center, int age, int index) => center +
        new Vector2(MathF.Sin(age * 1.71f + index * 2.1f) * (65 + age * .18f),
            MathF.Cos(age * 2.13f + index) * (42 + age * .10f));
    internal static float Hash(int seed)
    {
        float value = MathF.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
        return value - MathF.Floor(value);
    }
    internal static Color Tint(int index) => (index % 4) switch
    {
        0 => new Color(70, 237, 255), 1 => new Color(255, 169, 64),
        2 => new Color(192, 140, 255), _ => new Color(224, 255, 244)
    };
}
