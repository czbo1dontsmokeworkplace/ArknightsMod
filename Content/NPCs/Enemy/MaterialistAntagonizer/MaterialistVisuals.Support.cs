using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawSupport(MaterialistAntagonizer boss)
    {
        if (boss.SupportMode == 0) return;
        Color green = new(62, 255, 143);
        int age = boss.SupportAge;
        Vector2 muzzle = boss.CoreMuzzle;
        if (boss.SupportMode == 1)
        {
            float charge = MathHelper.Clamp(age / 42f, 0, 1);
            if (age < 42)
            {
                Ring(muzzle, 60 - charge * 30, green, age * .04f, 3, .65f, 48);
                Glow(muzzle, green * (.25f + charge * .55f), new Vector2(75 + charge * 45));
            }
            else
            {
                float radius = boss.RepairRadius;
                float fade = 1 - MathHelper.Clamp((age - 90) / 22f, 0, 1);
                Ring(boss.SupportOrigin, radius, green * fade, 0, 12);
                Ring(boss.SupportOrigin, Math.Max(0, radius - 13), Color.White * (.7f * fade), 0, 2);
                Ring(boss.SupportOrigin, Math.Max(0, radius - 40), green * (.35f * fade), 0, 22);
                if (!Reduced)
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 axis = (i * MathHelper.TwoPi / 24 + age * .002f).ToRotationVector2();
                        Vector2 tip = boss.SupportOrigin + axis * radius;
                        Line(tip, tip - axis * 24, green * fade, 3);
                    }
            }
        }
        else if (boss.RepairTarget is NPC target)
        {
            Brackets(target.Center, target.Size + new Vector2(40), green, .95f);
            Vector2 axis = (target.Center - muzzle).SafeNormalize(Vector2.UnitY);
            if (age < 36)
            {
                Line(muzzle, target.Center, green * .25f, 2);
                Ring(muzzle, 42 - age * .6f, green, age * .035f, 2);
                Reticle(target.Center, target.width * .7f + 20, green * .8f, age);
            }
            else if (age < 80)
            {
                float strength = MaterialistFormation.Smooth((age - 36) / 8f) * (1 - MaterialistFormation.Smooth((age - 68) / 12f));
                Vector2 middle = (muzzle + target.Center) * .5f;
                float length = Vector2.Distance(muzzle, target.Center);
                Glow(middle, green * (.75f * strength), new Vector2(length + 35, 76), axis.ToRotation());
                Line(muzzle, target.Center, (green with { A = 0 }) * strength, 30 + MathF.Sin(age * .6f) * 3);
                Line(muzzle, target.Center, (Color.White with { A = 0 }) * strength, 7);
                Glow(target.Center, green * strength, target.Size * 2 + new Vector2(45));
                Ring(target.Center, target.width * .75f + 18, green * strength, age * .025f, 4);
                foreach (int pulseAge in new[] { 48, 72 })
                {
                    float pulse = (age - pulseAge) / 8f;
                    if (pulse is >= 0 and < 1)
                    {
                        Ring(target.Center, target.width * (.65f + pulse * .6f), green * (1 - pulse), 0, 6);
                        Glow(target.Center, Color.White * ((1 - pulse) * .6f), target.Size * (1.5f + pulse));
                    }
                }
            }
        }
    }
}
