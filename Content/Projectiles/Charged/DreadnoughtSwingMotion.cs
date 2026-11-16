using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Charged;

// Profession-specific motion. Other professions retain their own shared curve.
internal static class DreadnoughtSwingMotion
{
    internal const float ReadyAngle = -MathHelper.Pi / 6;
    internal const float BackEnd = .29f, CutStart = .40f, CutEnd = .65f;
    private static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static float Offset(float progress) {
        float p = Math.Clamp(progress, 0, 1);
        if (p < .08f) return ReadyAngle;
        if (p < BackEnd) return ReadyAngle - MathHelper.Pi * Ease((p - .08f) / (BackEnd - .08f));
        if (p < CutStart) return ReadyAngle - MathHelper.Pi;
        // 180 degrees back, a brief stop, then 105 degrees through the low cut.
        // The blade finishes 45 degrees down, for a 285-degree attacking arc.
        float end = -MathHelper.TwoPi + MathHelper.PiOver4;
        if (p < CutEnd) return MathHelper.Lerp(ReadyAngle - MathHelper.Pi, end, Ease((p - CutStart) / (CutEnd - CutStart)));
        if (p < .82f) return end;
        // Harmless recovery takes the short path back to the 30-degree guard.
        return MathHelper.Lerp(end, ReadyAngle - MathHelper.TwoPi, Ease((p - .82f) / .18f));
    }
    internal static float Angle(float aim, int facing, float progress) => aim + Offset(progress) * facing;
    internal static bool Striking(float p) => p >= .08f && p < BackEnd || p >= CutStart && p < CutEnd;
    internal static float Intensity(float p) {
        if (!Striking(p)) return 0;
        float lo = p < BackEnd ? .08f : CutStart, hi = p < BackEnd ? BackEnd : CutEnd;
        return MathF.Sin(MathHelper.Pi * (p - lo) / (hi - lo));
    }
}
