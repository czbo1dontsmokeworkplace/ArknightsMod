using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawLockShield(MaterialistAntagonizer boss)
    {
        float progress = boss.LockShieldProgress;
        if (progress <= .001f) return;
        float opacity = MaterialistFormation.Smooth(progress);
        Vector2 center = boss.NPC.Center, size = boss.LockShieldSize, half = size * .5f;
        float rotation = boss.NPC.rotation * .35f;
        Color color = new(90, 213, 255);
        bool material = MaterialistTechRenderer.LockShieldMaterial(center, size, rotation, progress, color * opacity);
        Glow(center, color * (.10f * opacity), size * 1.06f, rotation);

        // The partial ellipse remains readable without the shader or with reduced effects.
        int segments = Reduced ? 40 : 80;
        for (int i = 0; i < segments; i++)
        {
            float a = i * MathHelper.TwoPi / segments, b = (i + 1) * MathHelper.TwoPi / segments;
            float midY = (MathF.Sin((a + b) * .5f) + 1) * .5f;
            if (midY > progress) continue;
            float packet = .65f + .35f * MathF.Sin((float)Main.GlobalTimeWrappedHourly * 2.2f - a * 4);
            Vector2 from = center + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * half).RotatedBy(rotation);
            Vector2 to = center + (new Vector2(MathF.Cos(b), MathF.Sin(b)) * half).RotatedBy(rotation);
            Line(from, to, (color with { A = 0 }) * (opacity * packet * .8f), 2.5f);
        }
        if (!material)
            for (int row = -3; row <= 3; row++)
            {
                float y = row / 4f;
                if ((y + 1) * .5f > progress) continue;
                float x = MathF.Sqrt(Math.Max(0, 1 - y * y)) * half.X;
                Line(center + new Vector2(-x, y * half.Y).RotatedBy(rotation),
                    center + new Vector2(x, y * half.Y).RotatedBy(rotation), color * (.13f * opacity), 1);
            }
        if (progress is > .015f and < .985f)
        {
            float y = progress * 2 - 1;
            float x = MathF.Sqrt(Math.Max(0, 1 - y * y)) * half.X;
            Vector2 left = center + new Vector2(-x, y * half.Y).RotatedBy(rotation);
            Vector2 right = center + new Vector2(x, y * half.Y).RotatedBy(rotation);
            Line(left, right, (color with { A = 0 }) * (opacity * .8f), 3);
            Glow((left + right) * .5f, color * (.25f * opacity), new Vector2(x * 2, 15), rotation);
        }
    }
}
