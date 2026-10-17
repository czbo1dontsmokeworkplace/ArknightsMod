using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class MelaniteFluxVfx
{
    // One damaging arrow keeps its twin spiral identity. Soft, short-lived fragments
    // are released into world space instead of drawing a rigid pair of rails on the shaft.
    internal static void Emit(Projectile arrow, Vector2 previous, Vector2 aim, int frame)
    {
        Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
        float distance = Vector2.Distance(previous, arrow.Center);
        int samples = Math.Clamp((int)MathF.Ceiling(distance / 14f), 1, 4);
        for (int i = 0; i < samples; i++)
        {
            float t = (i + .5f) / samples;
            float phase = (frame - 1f + t) * .5f + arrow.identity * .17f;
            Vector2 at = Vector2.Lerp(previous, arrow.Center, t) - aim * 8f;
            float radius = MathHelper.Clamp((frame + t) / 4f, 0f, 1f) * 9f;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 point = at + side * (MathF.Sin(phase) * radius * sign);
                Vector2 velocity = -aim * .7f + side * (MathF.Cos(phase) * sign * .7f);
                Color color = Color.Lerp(new(104, 40, 187), new(214, 156, 255),
                    .5f + .5f * MathF.Cos(phase) * sign);
                CrossbowParticles.Spark(point, velocity, color, 15f, 4.5f, 8);
            }
        }
        if (frame % 5 == 0)
            CrossbowParticles.Wisp(arrow.Center - aim * 13f, -aim * .4f,
                new(119, 61, 164), 19f, 16, .18f);
    }
}
