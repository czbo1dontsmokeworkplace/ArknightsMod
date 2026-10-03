using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

// All unspecified timings and support-unit numbers are tuning values, not additions to the move set.
internal static class DuoRules
{
    internal const int MephistoMasterLife = 6000, FaustMasterLife = 4000, MasterAttack = 200;
    internal const int HealInterval = 180, HostInterval = 600, TurretInterval = 480;
    internal const int TurretShotInterval = 300, HiddenAimTicks = 30, RetreatTicks = 90;
    internal const int DustDuration = 180, DustInterval = 15, DustDamage = 5;
    internal const float EscapeRadius = 1900, WarningRadius = 1500;
    internal const int EscapeGrace = 180, ExecutionLock = 90;
    internal static int ShotInterval(bool phaseTwo) => phaseTwo ? 90 : 150;
    internal static int Life(int master, bool expert, bool masterMode, float balance = 1) =>
        Math.Max(1, (int)(master * (masterMode ? 1f : expert ? .75f : .5f) * balance));
    internal static int Attack => Main.masterMode ? MasterAttack : Main.expertMode ? 150 : 100;
    internal static float TurretMultiplier(int count) => Math.Max(0, 1 - .1f * count);
    internal static bool StrongShot(int shot) => shot % 3 == 2;
    internal static bool AimVisible(int timer, bool phaseTwo) => timer < ShotInterval(phaseTwo) - HiddenAimTicks;
    internal static int EscapeTimer(int previous, float distance) => previous >= EscapeGrace || distance > EscapeRadius ? previous + 1 : Math.Max(0, previous - 4);

    // Swept box intersection: return the first contact along a segment, including its starting point.
    // This also prevents fast arrows/medicine from tunnelling through a player between updates.
    internal static bool SegmentHit(Vector2 start, Vector2 end, Rectangle box, float padding, out float time)
    {
        Vector2 delta = end - start;
        float enter = 0, leave = 1;
        bool Axis(float origin, float motion, float low, float high)
        {
            if (Math.Abs(motion) < .0001f) return origin >= low && origin <= high;
            float a = (low - origin) / motion, b = (high - origin) / motion;
            if (a > b) (a, b) = (b, a);
            enter = Math.Max(enter, a); leave = Math.Min(leave, b);
            return enter <= leave;
        }
        bool result = Axis(start.X, delta.X, box.Left - padding, box.Right + padding) &&
                      Axis(start.Y, delta.Y, box.Top - padding, box.Bottom + padding);
        time = enter;
        return result;
    }
}
