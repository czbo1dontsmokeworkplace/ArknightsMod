namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// Real ticks: attack-speed modifiers affect pulses, never the charge/burst duration.
internal static class GoldenglowLightningBalance
{
    internal const int Damage = 260; // 338 / 1.3: preserve nominal damage per second.
    internal const float FrequencyMultiplier = 1.3f;
    internal const int ChargeTicks = 150;
    internal const int BurstTicks = 36;
    internal const int NormalPulseTicks = 12;
    internal const int BurstPulseTicks = 6;
    internal const int ManaInterval = 12;
    internal const int ManaPerPayment = 6;
    internal const float BurstDamage = 1.8f;
    internal const float Range = 1000f;
    internal const float WaterRadius = 500f;
    internal const int WaterLifetime = 48;

    internal static bool IsBurst(int tick) => tick % (ChargeTicks + BurstTicks) >= ChargeTicks;
    internal static float Charge(int tick) => System.Math.Clamp(
        tick % (ChargeTicks + BurstTicks) / (float)ChargeTicks, 0f, 1f);
}
