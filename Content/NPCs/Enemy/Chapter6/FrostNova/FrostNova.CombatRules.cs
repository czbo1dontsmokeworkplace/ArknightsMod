using System;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// All values are per real game tick, not per projectile sub-update.
internal static class FrostNovaCombatRules
{
    internal const float FlightSpeed = 11.8f, DashSpeed = 21.6f, ShardSpeed = 6.4f, LegacyShardSpeed = 10.2f;
    internal const float GroundDashSpeed = 18f, AirDashSpeed = 21.6f, SlamSpeed = 21f, BarrageSpeedCap = 9.2f;
    internal const int BarrageUpdates = 2;
    internal const int HazardCap = 128, SupportCap = 5, SmallDamage = 21, HeavyDamage = 26;
    internal const int BirthDuration = 240, RevivalDuration = 600, PhaseTwoEntrance = 150;
    internal const int FreezeCooldown = 210, Recovery = 36;
    private static readonly int[] GroundCycle = { 0, 1, 9, 2, 3, 4, 0, 5, 9, 0 };
    private static readonly int[] AirCycle = { 1, 6, 2, 8, 4, 7, 5, 9, 0, 3, 1, 8, 4, 10, 2, 7, 5, 6, 0, 3 };
    internal static int Pattern(int phase, int serial) {
        int[] cycle = phase == 2 ? AirCycle : GroundCycle;
        return cycle[(serial % cycle.Length + cycle.Length) % cycle.Length];
    }
    internal static int BaseAttackDuration(int phase, int pattern) => pattern switch {
        0 => phase == 2 ? 180 : 210,
        1 or 2 or 4 or 5 => phase == 2 ? 246 : 276,
        3 => 210,
        6 => 210,
        7 or 8 => 240,
        9 => 270,
        10 => 240,
        _ => 240
    };
    // Retain every warned formation and its projectile speed. Longer recovery
    // reduces phase-two attack frequency by 25%, rather than randomly deleting safe lanes.
    internal static int AttackDuration(int phase, int pattern) => phase == 2
        ? (int)Math.Ceiling(BaseAttackDuration(phase, pattern) * (4d / 3)) : BaseAttackDuration(phase, pattern);
    internal static bool InRecovery(int phase, int pattern, int timer) => timer >= BaseAttackDuration(phase, pattern) - Recovery;
    internal static float RingGap(int phase) => phase == 2 ? .28f : .34f;
    internal static int ShotDamage(int phase, bool heavy = false) => (heavy ? HeavyDamage : SmallDamage) - (phase == 1 ? 2 : 0);
    internal static bool IsMotion(int pattern) => pattern is 6 or 9 or 10;
    internal static bool PressureBeat(int phase, int pattern, int timer) => !IsMotion(pattern)
        && timer >= (phase == 2 ? 84 : 114) && timer % (phase == 2 ? 96 : 126) == (phase == 2 ? 84 : 114)
        && !InRecovery(phase, pattern, timer);
    internal static float FormationAngle(int index, int count, float gapCenter) => gapCenter + MathF.PI * 2 * index / Math.Max(1, count);
    internal static bool InGap(float angle, float center, float halfWidth) {
        float wrapped = MathF.IEEERemainder(angle - center, MathF.PI * 2);
        return MathF.Abs(wrapped) < halfWidth;
    }
}
