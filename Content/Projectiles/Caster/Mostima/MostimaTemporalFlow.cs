using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// All paths live inside 550 world pixels (before skill scale). Their positions and
// tangents are continuous. Neither attack beats nor the hands enter these equations.
internal static class MostimaTemporalFlow
{
    internal static Vector2 Point(int mode, float angle, float hand, float t, float scale)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        float radius;
        if (mode == 0)
        {
            // Perspective-like acceleration out of the dial, without a singularity.
            radius = 38f + 492f * (MathF.Exp(1.8f * t) - 1f) / (MathF.Exp(1.8f) - 1f);
            angle += hand * .12f * t;
        }
        else if (mode == 1)
        {
            radius = 530f - 492f * t * t;
            angle -= hand * .2f * t;
        }
        else
        {
            // Logarithmic spiral: constant relation between angular travel and log(r).
            radius = 510f * MathF.Exp(-1.85f * t);
            angle += hand * 2.8f * t;
        }
        return angle.ToRotationVector2() * (radius * scale);
    }

    internal static Color Tint(int mode, float gold) => MostimaClockwork.TimeColor(mode switch
    {
        0 => new Color(107, 210, 255),
        1 => new Color(183, 145, 249),
        _ => new Color(242, 204, 138)
    }, gold);
}
