using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static bool DrawPayload(MaterialistHazard h, Color color)
    {
        Projectile p = h.Projectile;
        if (h.Kind == DroneShot.DropMissile)
        {
            if (h.Age < h.Delay) Brackets(p.Center, new Vector2(30, 48), color, .8f);
            DrawVanillaRocket(h, color);
            return true;
        }
        if (h.Kind == DroneShot.ScanBurst)
        {
            Vector2 size = h.ScanSize, half = size * .5f;
            float charge = MathHelper.Clamp(h.Age / (float)Math.Max(1, h.Delay), 0, 1);
            if (h.Age < h.Delay)
            {
                Brackets(p.Center, size, color, .6f + .35f * charge);
                Brackets(p.Center, size + new Vector2(18), White, .4f + charge * .4f);
                float scan = MathHelper.Lerp(-half.Y, half.Y, charge);
                Glow(p.Center + new Vector2(0, scan), color * .7f, new Vector2(size.X + 30, 30));
                Line(p.Center + new Vector2(-half.X, scan), p.Center + new Vector2(half.X, scan), White * .85f, 3);
                float pulse = .6f + MathF.Sin(h.Age * .3f) * .15f;
                Vector2 leftTop = p.Center - half, rightTop = p.Center + new Vector2(half.X, -half.Y);
                Vector2 leftBottom = p.Center + new Vector2(-half.X, half.Y), rightBottom = p.Center + half;
                Line(leftTop, rightTop, (color with { A = 0 }) * pulse, 3);
                Line(rightTop, rightBottom, (color with { A = 0 }) * pulse, 3);
                Line(rightBottom, leftBottom, (color with { A = 0 }) * pulse, 3);
                Line(leftBottom, leftTop, (color with { A = 0 }) * pulse, 3);
                for (float x = -half.X; x <= half.X; x += 32)
                    Line(p.Center + new Vector2(x, -half.Y), p.Center + new Vector2(x, half.Y), color * .24f, 1);
                for (float y = -half.Y; y <= half.Y; y += 32)
                    Line(p.Center + new Vector2(-half.X, y), p.Center + new Vector2(half.X, y), color * .24f, 1);
                float inset = 8 + (1 - charge) * 24;
                Brackets(p.Center, size - new Vector2(inset), color, .20f);
                if (charge > .72f) Glow(p.Center, color * (.2f * (charge - .72f) / .28f), size);
            }
            else
            {
                float fade = MathHelper.Clamp((h.Delay + h.Duration - h.Age) / (float)h.Duration, 0, 1);
                Brackets(p.Center, size, White, fade);
                Glow(p.Center, color * (.8f * fade), size * 1.25f);
                // Bright plane and collapsing grid make the damaging rectangle explicit.
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 a = p.Center + new Vector2(i * half.X / 3, -half.Y);
                    Line(a, a + new Vector2(0, size.Y), (color with { A = 0 }) * fade, 4 * fade + 1);
                }
                Line(p.Center - new Vector2(half.X, 0), p.Center + new Vector2(half.X, 0), White * fade, 7 * fade);
            }
            return true;
        }
        if (h.Kind == DroneShot.Beacon)
        {
            float hover = h.Age < 45 ? 1 : 1 - MathHelper.Clamp((h.Age - 150) / 14f, 0, 1);
            float turn = h.Age * .035f;
            Ring(p.Center, 10, color * hover, MathHelper.PiOver4, 3, 1, 4);
            Ring(p.Center, 18, color * (.65f * hover), turn, 1, .65f, 12);
            Glow(p.Center, White * (.8f * hover), new Vector2(9));
            if (h.Age >= 45)
            {
                Vector2 top = new(p.Center.X, Math.Max(48, p.Center.Y - 880));
                for (float y = top.Y; y < p.Center.Y + 350; y += 48)
                {
                    Vector2 a = new(p.Center.X, y);
                    Line(a, a + Vector2.UnitY * 25, color * (.35f * hover), 2);
                }
                for (int side = -1; side <= 1; side += 2)
                    Line(top + new Vector2(side * 12, 0), p.Center + new Vector2(side * 12, 260), color * (.14f * hover), 1);
                Brackets(p.Center, new Vector2(58, 28), color, hover * .85f);
                if (h.Bend > .5f)
                    Ring(p.Center, MaterialistRules.BombRadius, color * (.65f * hover), 0, 2);
                else
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 tip = p.Center + new Vector2(0, 40 + i * 32);
                        Line(tip - new Vector2(9, 9), tip, color * (.7f * hover), 2);
                        Line(tip - new Vector2(-9, 9), tip, color * (.7f * hover), 2);
                    }
            }
            return true;
        }
        if (h.Kind != DroneShot.GravityOrb) return false;
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY), n = axis.RotatedBy(MathHelper.PiOver2);
        if (h.Age < h.Delay)
        {
            Brackets(p.Center, new Vector2(30, 48), color, .8f);
            return true;
        }
        DrawGravityStreak(h);
        return true;
    }
}
