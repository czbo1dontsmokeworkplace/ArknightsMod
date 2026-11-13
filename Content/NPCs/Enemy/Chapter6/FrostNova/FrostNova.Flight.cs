namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

internal static class FrostNovaFlightRules
{
    // A committed heading: fast launch, readable braking, then renewed forward pressure.
    // Multipliers are applied once per real tick and replay from synchronized age.
    internal static float CurvedFactor(int age) => age <= 45 ? .985f : age <= 120 ? 1.014f : 1.006f;
    internal static float SpiralSpeed(float initial, int age) => initial * 1.15f
        * System.MathF.Pow(.984f, System.Math.Min(age, 30))
        * System.MathF.Pow(1.01f, System.Math.Max(0, age - 30));
}
