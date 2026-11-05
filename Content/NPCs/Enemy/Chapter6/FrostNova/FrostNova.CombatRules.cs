using System;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// All values are per real game tick, not per projectile sub-update.
internal static class FrostNovaCombatRules
{
    internal const float FlightSpeed = 10.5f, DashSpeed = 19.6f, ShardSpeed = 5.2f, LegacyShardSpeed = 9.8f;
    internal const int HazardCap = 112, SupportCap = 5, SmallDamage = 21, HeavyDamage = 26;
    internal const int BirthDuration = 240, RevivalDuration = 600, PhaseTwoEntrance = 150;
    internal const int FreezeCooldown = 210, Recovery = 54;
    private static readonly int[] GroundCycle = { 0, 1, 0, 2, 3, 4, 0, 5 };
    private static readonly int[] AirCycle = { 1, 6, 2, 8, 4, 7, 5, 0, 3, 6, 8, 4 };
    internal static int Pattern(int phase, int serial) {
        int[] cycle = phase == 2 ? AirCycle : GroundCycle;
        return cycle[(serial % cycle.Length + cycle.Length) % cycle.Length];
    }
    internal static int AttackDuration(int phase, int pattern) => pattern switch {
        0 => phase == 2 ? 210 : 240,
        1 or 2 or 4 or 5 => phase == 2 ? 330 : 360,
        3 => 240,
        6 => 246,
        7 => 300,
        8 => 300,
        _ => 240
    };
    internal static bool InRecovery(int phase, int pattern, int timer) => timer >= AttackDuration(phase, pattern) - Recovery;
    internal static float RingGap(int phase) => phase == 2 ? .28f : .34f;
    internal static int ShotDamage(int phase, bool heavy = false) => (heavy ? HeavyDamage : SmallDamage) - (phase == 1 ? 2 : 0);
    internal static float FormationAngle(int index, int count, float gapCenter) => gapCenter + MathF.PI * 2 * index / Math.Max(1, count);
    internal static bool InGap(float angle, float center, float halfWidth) {
        float wrapped = MathF.IEEERemainder(angle - center, MathF.PI * 2);
        return MathF.Abs(wrapped) < halfWidth;
    }
}
