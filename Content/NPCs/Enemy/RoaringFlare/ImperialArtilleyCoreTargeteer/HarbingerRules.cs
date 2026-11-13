using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

// Shared by the encounter, its weapon and the isolated rule checks.
internal static class HarbingerRules
{
    internal const int TransitionTicks = 110, DeathTicks = 180, HazardCap = 240, PatternCount = 6;
    internal static int Phase(float fraction) => fraction > .7f ? 1 : fraction > .3f ? 2 : 3;
    internal static int CycleLength(int phase) => phase == 1 ? 420 : phase == 2 ? 360 : 330;
    internal static float Hack(int phase, int pattern, int clock, bool transition)
    {
        if (phase != 3 || transition) return 0;
        int start = pattern % PatternCount == 0 ? 145 : pattern % PatternCount == 2 ? 185 : -1000;
        int elapsed = clock - start;
        if (elapsed < 0 || elapsed >= 95) return 0;
        return Math.Min(1, Math.Min((elapsed + 1) / 10f, (95 - elapsed) / 22f));
    }
    internal static Vector2 WeakHoming(Vector2 velocity, Vector2 delta, float maxTurn = .0035f)
    {
        float speed = velocity.Length();
        if (speed < .01f || delta.LengthSquared() < 1) return velocity;
        float angle = MathHelper.WrapAngle(delta.ToRotation() - velocity.ToRotation());
        return velocity.RotatedBy(Math.Clamp(angle, -maxTurn, maxTurn));
    }
    internal static bool BeamClear(Vector2 start, Vector2 end, Vector2 player, float clearance = 150)
    {
        Vector2 axis = end - start;
        float t = axis.LengthSquared() < 1 ? 0 : Math.Clamp(Vector2.Dot(player - start, axis) / axis.LengthSquared(), 0, 1);
        return Vector2.DistanceSquared(player, start + axis * t) >= clearance * clearance;
    }
    internal static bool Harmful(int kind, int age) => kind is 1 or 3 or 5 ? age >= 8 : kind == 2 && age is >= 1 and < 3;
    internal static string Corrupt(string source, int tick, int salt = 0)
    {
        const string glyphs = "01ABCDEF!?%#$@/\\<>";
        if (string.IsNullOrEmpty(source)) return source;
        char[] result = source.ToCharArray();
        for (int i = 0; i < result.Length; i++) {
            if (char.IsWhiteSpace(result[i])) continue;
            uint n = unchecked((uint)((tick * 374761393) ^ (i * 668265263) ^ salt));
            n = (n ^ (n >> 13)) * 1274126177u;
            result[i] = glyphs[(int)((n ^ (n >> 16)) % glyphs.Length)];
        }
        return new string(result);
    }
}
