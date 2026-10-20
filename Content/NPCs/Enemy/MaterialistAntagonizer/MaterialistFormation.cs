using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static class MaterialistFormation
{
    internal static float Smooth(float t) { t = MathHelper.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    internal static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        t = MathHelper.Clamp(t, 0, 1); float u = 1 - t;
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
    }
    internal static bool CrossesExit(Vector2 origin, Vector2 velocity, Vector2 center, float exit, int lifetime = 170)
    {
        Vector2 axis = exit.ToRotationVector2(), normal = axis.RotatedBy(MathHelper.PiOver2);
        for (int t = 0; t <= lifetime; t += 2)
        {
            Vector2 p = origin + velocity * t - center;
            float forward = Vector2.Dot(p, axis);
            if (forward > 30 && forward < 720 && Math.Abs(Vector2.Dot(p, normal)) < 90) return true;
        }
        return false;
    }
}
