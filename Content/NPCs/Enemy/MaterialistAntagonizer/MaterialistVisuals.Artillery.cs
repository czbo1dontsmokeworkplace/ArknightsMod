using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static bool DrawArtilleryAmmo(MaterialistHazard h, Color color)
    {
        Projectile p = h.Projectile;
        if (h.Kind == DroneShot.ArtilleryBlast)
        {
            float radius = h.Length;
            if (h.Age < h.Delay)
            {
                float charge = h.Age / (float)Math.Max(1, h.Delay);
                Ring(p.Center, radius, color * .9f, 0, 2);
                Ring(p.Center, radius * (1.4f - .4f * charge), White * .45f, 0, 1);
                Brackets(p.Center, new Vector2(30), color, .85f);
                Glow(p.Center, color * .18f, new Vector2(radius));
            }
            else
            {
                float age = h.Age - h.Delay, progress = MathHelper.Clamp(age / 22f, 0, 1), fade = 1 - progress;
                // The hit boundary remains explicit; shock spokes are decorative and stay inside it.
                Ring(p.Center, radius, color * fade, 0, 3);
                Ring(p.Center, radius * MathF.Sqrt(progress), White * fade, 0, 4);
                Ring(p.Center, radius * .68f, color * (.7f * fade), age * .03f, 2, 1, 8);
                Glow(p.Center, color * (.75f * fade), new Vector2(radius * 2.2f));
                Glow(p.Center, White * fade, new Vector2(radius * 1.05f, radius * .4f));
                for (int i = 0; i < (Reduced ? 6 : 12); i++)
                {
                    Vector2 axis = (i * MathHelper.TwoPi / (Reduced ? 6 : 12) + p.identity * .37f).ToRotationVector2();
                    float distance = radius * (.25f + progress * .6f);
                    Line(p.Center + axis * distance, p.Center + axis * Math.Min(radius, distance + 18 * fade), (White with { A = 0 }) * fade, 2);
                }
            }
            return true;
        }
        if (h.Kind is not (DroneShot.CommandRocket or DroneShot.CannonShell)) return false;
        Vector2 axisForward = p.velocity.SafeNormalize(Vector2.UnitY), normal = axisForward.RotatedBy(MathHelper.PiOver2);
        for (int i = p.oldPos.Length - 1; i > 0; i--)
            if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
            {
                float fade = 1 - i / (float)p.oldPos.Length;
                Line(p.oldPos[i] + p.Size * .5f, p.oldPos[i - 1] + p.Size * .5f,
                    (color with { A = 0 }) * (fade * .55f), (h.Kind == DroneShot.CommandRocket ? 5 : 3) * fade);
            }
        if (h.Kind == DroneShot.CannonShell)
        {
            // The same discrete gravity equation drives the preview and actual projectile flight.
            if (!h.Retiring)
            {
                float remaining = 1 - MathHelper.Clamp((h.Age - h.Delay) / (float)h.ShellFlight, 0, 1);
                Ring(h.Anchor, h.Length, color * (.65f + .25f * MathF.Sin(h.Age * .22f)), 0, 2);
                Brackets(h.Anchor, new Vector2(h.Length * 1.4f), color, .6f);
                Ring(h.Anchor, 10 + remaining * 22, White * .65f, 0, 1);
                if (!Reduced)
                    for (int step = Math.Max(0, h.Age - h.Delay + 3); step < h.ShellFlight - 3; step += 7)
                        Line(h.ShellPoint(step), h.ShellPoint(Math.Min(step + 3, h.ShellFlight)), color * .23f, 1);
            }
            Vector2 rear = p.Center - axisForward * 10, front = p.Center + axisForward * 9;
            Line(rear, front, new Color(91, 112, 128), 13);
            Line(rear + normal * 3, front + normal * 3, new Color(209, 223, 226), 3);
            Line(front + normal * 6, front + axisForward * 7, color, 3);
            Line(front - normal * 6, front + axisForward * 7, color, 3);
            Line(rear + axisForward * 2 - normal * 6, rear + axisForward * 2 + normal * 6, color, 3);
            Glow(p.Center, color * .22f, new Vector2(32, 23), p.rotation);
        }
        else
        {
            DrawVanillaRocket(h, color);
            if (h.Age < 28) Brackets(p.Center, new Vector2(34), Cyan, .65f);
            else if (!h.RocketCommitted && !h.Retiring) Ring(p.Center, 20, color * .4f, h.Age * .05f, 1, .55f, 8);
        }
        return true;
    }
    private static void DrawArtilleryBattery(MaterialistAntagonizer boss)
    {
        int beat = MaterialistTactics.Beat(boss.Timer);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 port = boss.RocketPort(side);
            if (boss.ArtilleryPlan is ArtilleryPattern.RocketRelay or ArtilleryPattern.Crossfire)
            {
                Brackets(port, new Vector2(39, 23), Cyan, .8f);
                int launched = Math.Clamp((beat - 10) / boss.RocketCadence + (beat >= 10 ? 1 : 0), 0, boss.RocketCount);
                for (int i = 0; i < boss.RocketCount; i++)
                {
                    Vector2 point = port + new Vector2((i - (boss.RocketCount - 1) * .5f) * 4, 14);
                    Line(point, point - Vector2.UnitY * 4, i < launched ? Amber * .8f : Cyan * .4f, 2);
                }
                if (beat < 94)
                    Glow(port, Cyan * (.20f + MathF.Sin(beat * .35f) * .1f), new Vector2(35, 18), boss.NPC.rotation);
            }
            if (boss.ArtilleryPlan is ArtilleryPattern.WalkingBarrage or ArtilleryPattern.Crossfire)
            {
                Vector2 cannon = boss.CannonPort(side);
                int next = -1;
                for (int b = beat; b <= beat + 22 && b < 180; b++)
                    if (boss.CannonBeat(b)) { next = b; break; }
                float charge = next < 0 ? 0 : 1 - (next - beat) / 23f;
                Ring(cannon, 18, Amber * .7f, boss.NPC.rotation, 2, .7f, 8);
                Ring(cannon, 29 - charge * 11, Cyan * (.3f + charge * .5f), -beat * .05f, 1, .7f, 12);
                Glow(cannon, Amber * (charge * .7f), new Vector2(25 + charge * 20));
            }
        }
        if (beat is >= 104 and < 132)
        {
            Vector2 axis = (boss.CoreAim - boss.CoreMuzzle).SafeNormalize(Vector2.UnitY);
            for (int i = -1; i <= 1; i++)
                Line(boss.CoreMuzzle, boss.CoreMuzzle + axis.RotatedBy(i * .16f * MaterialistRules.SpreadSpacing) * 850, Amber * .28f, 1);
            Reticle(boss.CoreAim, 18, Amber * .7f, boss.Timer);
        }
    }
}
