using System;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static bool DrawEscortAmmo(MaterialistHazard h)
    {
        if (h.Kind is not (DroneShot.ScoutPulse or DroneShot.ArtsOrb)) return false;
        Projectile p = h.Projectile;
        float fade = h.Visibility * MathHelper.Clamp((h.Age + 1) / 8f, 0, 1);
        if (h.Age < h.Delay)
        {
            float charge = h.Age / (float)Math.Max(1, h.Delay);
            Glow(p.Center, (h.Kind == DroneShot.ArtsOrb ? Violet * .7f : Amber) * (.25f + charge * .3f), new Vector2(28 + charge * 15));
            return true;
        }
        if (h.Kind == DroneShot.ScoutPulse)
        {
            int count = 1;
            while (count < p.oldPos.Length && p.oldPos[count - 1] != Vector2.Zero) count++;
            Vector2[] points = new Vector2[count];
            points[0] = p.Center;
            for (int i = 1; i < count; i++) points[i] = p.oldPos[i - 1] + p.Size * .5f;
            // Reuse Rockrock's actual material, ribbon and head; only the enemy flight AI differs.
            MechAccordVfx.DrawTrail(points, fade, 1.7f);
            MechAccordVfx.DrawStar(p.Center, p.rotation, fade, 1.7f);
        }
        else if (h.Retiring)
        {
            // Active light is the exact 70% bloom pair emitted by AI. Keep a light-only head during retirement.
            Color purple = new(177, 101, 255);
            Glow(p.Center, purple * (.7f * .55f), new Vector2(64, 45), p.rotation);
            Glow(p.Center, Color.Lerp(purple, Color.White, .6f) * .7f, new Vector2(26, 18), p.rotation);
        }
        return true;
    }
}
