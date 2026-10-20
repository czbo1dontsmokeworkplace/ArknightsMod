using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static bool DrawMathematicalAmmo(MaterialistHazard h, Color color)
    {
        if (h.Kind is not (DroneShot.ArcSpark or DroneShot.Petal or DroneShot.Lattice or DroneShot.CoreLance or DroneShot.ClusterOrb or DroneShot.OrbitSpark or DroneShot.Needle)) return false;
        Projectile p = h.Projectile;
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY), n = axis.RotatedBy(MathHelper.PiOver2);
        if (h.Age < h.Delay)
        {
            Ring(p.Center, 14, color * .85f, h.Age * .07f, 2, 1, 4);
            if (h.Kind is DroneShot.CoreLance or DroneShot.Needle)
                Line(p.Center, p.Center + axis * 800, color * .32f, 1);
        }
        for (int i = p.oldPos.Length - 1; i >= 1; i--)
            if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
            {
                float fade = 1 - i / (float)p.oldPos.Length;
                Line(p.oldPos[i] + p.Size * .5f, p.oldPos[i - 1] + p.Size * .5f, (color with { A = 0 }) * (fade * .6f),
                    (h.Kind == DroneShot.CoreLance ? 5 : 3) * fade);
            }
        switch (h.Kind)
        {
            case DroneShot.CoreLance:
                Line(p.Center - axis * 22, p.Center + axis * 17, color with { A = 0 }, 5);
                Line(p.Center - axis * 14, p.Center + axis * 17, White with { A = 0 }, 2);
                Line(p.Center + axis * 17, p.Center + n * 7, color, 2);
                Line(p.Center + axis * 17, p.Center - n * 7, color, 2);
                Glow(p.Center, color * .55f, new Vector2(48, 18), p.rotation);
                break;
            case DroneShot.Needle:
                Line(p.Center - axis * 13, p.Center + axis * 11, color, 3);
                Line(p.Center - n * 5 - axis * 3, p.Center + axis * 6, Cyan, 1);
                Line(p.Center + n * 5 - axis * 3, p.Center + axis * 6, Cyan, 1);
                break;
            case DroneShot.Lattice:
                Ring(p.Center, 8, color, MathHelper.PiOver4, 2, 1, 4);
                Ring(p.Center, 4, White, MathHelper.PiOver4, 2, 1, 4);
                Glow(p.Center, color * .4f, new Vector2(24));
                break;
            case DroneShot.Petal:
                Vector2 tip = p.Center + axis * 12, tail = p.Center - axis * 10;
                Line(tail, p.Center + n * 7, color, 3);
                Line(p.Center + n * 7, tip, color, 2);
                Line(tail, p.Center - n * 7, color, 3);
                Line(p.Center - n * 7, tip, color, 2);
                Glow(p.Center, color * .5f, new Vector2(23));
                break;
            case DroneShot.ArcSpark:
                for (int i = 0; i < 4; i++)
                {
                    Vector2 a = p.Center - axis * (i * 4) + n * (i * i * .6f);
                    Vector2 b = p.Center - axis * ((i + 1) * 4) + n * ((i + 1) * (i + 1) * .6f);
                    Line(a, b, (color with { A = 0 }) * (1 - i * .16f), 4 - i * .6f);
                }
                Glow(p.Center, White * .7f, new Vector2(9));
                break;
            case DroneShot.ClusterOrb:
                float charge = MathHelper.Clamp((h.Age - h.Delay - 24) / 20f, 0, 1);
                Ring(p.Center, 14 + MathF.Sin(h.Age * .15f) * 2, color, -h.Age * .05f, 2, 1, 6);
                Glow(p.Center, color * .7f, new Vector2(34));
                Glow(p.Center, White * .9f, new Vector2(13));
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 d = axis.RotatedBy(i * .24f * MaterialistRules.SpreadSpacing);
                    Line(p.Center + d * 18, p.Center + d * (28 + charge * 24), color * (.2f + .5f * charge), 1);
                }
                break;
            case DroneShot.OrbitSpark:
                for (int i = 0; i < 4; i++)
                {
                    Vector2 d = (i * MathHelper.PiOver2 + h.Age * .1f).ToRotationVector2();
                    Line(p.Center - d * 10, p.Center + d * 10, color, i % 2 == 0 ? 3 : 2);
                }
                Glow(p.Center, White * .9f, new Vector2(10));
                break;
        }
        return true;
    }
}
