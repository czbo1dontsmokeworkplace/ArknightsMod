using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// One deterministic geometry shared by the warning, pressure material and server collision.
internal static class EvolutionShockwaveRules
{
    internal const int GapCount = 5, WarningTicks = 60, ReleaseTicks = 8;
    internal const int Duration = 96, DamageTicks = 72;
    internal const float Reach = 1560, GapHalfWidth = .24f;
    internal const int Interval = 382, WaveStart = 60, LaserStart = 260;
    internal static float GapAngle(float playerAngle, int cycle) => playerAngle + (cycle % 2 == 0 ? .38f : -.38f);
    internal static float GapDistance(float angle, float axis) => Math.Abs(MathF.IEEERemainder(angle - axis, MathHelper.TwoPi / GapCount));
    internal static bool Safe(float angle, float axis) => GapDistance(angle, axis) <= GapHalfWidth + .008f;
    internal static float Radius(int age) => EvolutionShockwaveVisuals.Radius(Reach, EvolutionShockwaveVisuals.Progress(age, Duration));
    internal static bool Collides(Rectangle box, Vector2 origin, float axis, int age)
    {
        if (age < 0 || age >= DamageTicks) return false;
        Vector2 delta = new Vector2(box.Center.X, box.Center.Y) - origin;
        // The player's center inside the advertised corridor always wins, including edge tolerance.
        if (delta.LengthSquared() > 1 && Safe(MathF.Atan2(delta.Y, delta.X), axis)) return false;
        float halfWidth = EvolutionShockwaveVisuals.Width(Reach, EvolutionShockwaveVisuals.Progress(age, Duration)) * .22f;
        float inner = Math.Max(0, Radius(Math.Max(0, age - 1)) - halfWidth), outer = Radius(age) + halfWidth;
        Vector2 nearest = new(Math.Clamp(origin.X, box.Left, box.Right), Math.Clamp(origin.Y, box.Top, box.Bottom));
        float dx = Math.Max(Math.Abs(box.Left - origin.X), Math.Abs(box.Right - origin.X));
        float dy = Math.Max(Math.Abs(box.Top - origin.Y), Math.Abs(box.Bottom - origin.Y));
        return Vector2.DistanceSquared(nearest, origin) <= outer * outer && dx * dx + dy * dy >= inner * inner;
    }
}
