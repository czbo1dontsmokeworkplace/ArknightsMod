namespace ArknightsMod.Content.NPCs.Friendly;

public static class CannotSupportMath
{
    // Carry fractional HP between pulses: ten pulses heal exactly floor(40% max HP).
    public static int HealForSecond(int maximumLife, int second) =>
        (int)((long)maximumLife * 4 * second / 100 - (long)maximumLife * 4 * (second - 1) / 100);

    // Terraria consumes this counter in batches of 120 per HP (or 120 * displayed DOT damage).
    public static int ClampUndyingRegen(int counter, int regeneration, int life) {
        long minimum = -(System.Math.Max(1, life) - 1L) * 120 - regeneration;
        return System.Math.Max(counter, (int)System.Math.Clamp(minimum, int.MinValue, int.MaxValue));
    }
}
