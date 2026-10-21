using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionCorridorPressureRules
{
    internal const float Speed = 1.3f;
    internal const int ApproachLimit = 25;
    internal static bool ShouldApproach(int time, float distance) => time >= 0 && time < ApproachLimit && distance > 1300;
    internal static bool ExtraShot(int ordinal) => ordinal > 0 && ordinal % 2 == 0;
    // FireTime(CrashFan) adds four frames; shorten the whole warning-to-hit interval.
    internal static int FanDelay(int delay) => Math.Max(1, (int)MathF.Round((delay + 4) / 1.3f) - 4);
    internal static bool IsFigure(int attack) => attack is 25 or 26;
    internal static Vector2 CompanionOffset(Vector2 origin, Vector2 velocity, Vector2 pattern, float top, float bottom)
    {
        Vector2 normal = velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        float signed = Vector2.Dot(origin - pattern, normal);
        float side = signed < 0 ? -1 : 1;
        Vector2 point = origin + normal * (side * 54);
        if (Math.Abs(velocity.X) > Math.Abs(velocity.Y) * 2)
        {
            // Additional horizontal rounds stay out of the snapshotted central flight lane.
            if (Math.Abs(point.Y - pattern.Y) < 96) point.Y = pattern.Y + (point.Y < pattern.Y ? -96 : 96);
            point.Y = Math.Clamp(point.Y, top + 32, bottom - 32);
        }
        return point - origin;
    }
}

public sealed partial class Evolution
{
    private void DoHorizontalPressure()
    {
        if (Attack == 25 && Timer is 12 or 72)
        {
            int wave = (Timer - 12) / 60, side = wave == 0 ? -1 : 1;
            // One direction per wave, with a fixed two-row passage. No simultaneous crossfire.
            for (int row = 0; row < 8; row++)
            {
                if (row is 3 or 4) continue;
                float y = MathHelper.Lerp(CorridorTop + 100, CorridorBottom - 100, row / 7f);
                Shoot(EvolutionShot.Beam, new Vector2(PatternOrigin.X + side * 950, y), -side * Vector2.UnitX,
                    2100, 36, 144, quietLaunch: true);
            }
        }
        if (Attack == 26 && Timer >= 12 && Timer <= 102 && (Timer - 12) % 30 == 0)
        {
            int wave = (Timer - 12) / 30, side = wave < 2 ? -1 : 1;
            int gap = (int)NPC.ai[2] % 2 == 0 ? 2 : 4;
            for (int row = 0; row < 8; row++)
            {
                if (row == gap || row == gap + 1) continue;
                float y = MathHelper.Lerp(CorridorTop + 80, CorridorBottom - 80, row / 7f);
                Shoot(EvolutionShot.Barrage, new Vector2(PatternOrigin.X + side * 800, y), -side * Vector2.UnitX * 10,
                    delay: 24, lifetime: 120, quietLaunch: true);
            }
        }
    }
}
