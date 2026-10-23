using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static bool DrawSpecialAmmo(MaterialistHazard h, Color color)
    {
        Projectile p = h.Projectile;
        if (h.GoldenFan) { DrawGoldenFan(h); return true; }
        if (DrawEscortAmmo(h)) return true;
        if (h.Kind == DroneShot.SaucerRound) { DrawSaucerRound(h); return true; }
        if (h.Kind == DroneShot.Missile) { DrawVanillaRocket(h, color); return true; }
        Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY), n = axis.RotatedBy(MathHelper.PiOver2);
        if (h.Kind == DroneShot.MachineRound) { MaterialistBallistics.DrawRound(h); return true; }
        if (DrawArtilleryAmmo(h, color)) return true;
        if (DrawPayload(h, color)) return true;
        if (DrawMathematicalAmmo(h, color)) return true;
        if (h.Kind is DroneShot.FluxMine or DroneShot.TrailBomb)
        {
            float charge = MathHelper.Clamp(h.Age / (float)Math.Max(1, h.Delay), 0, 1);
            float fade = h.Age < h.Delay ? .45f + .45f * charge : MathHelper.Clamp((h.Delay + h.Duration - h.Age) / 6f, 0, 1);
            // Full damaging radius is always visible, including in reduced-effects mode.
            Ring(p.Center, h.MineRadius, color * fade, 0, h.Age < h.Delay ? 1 : 4);
            Ring(p.Center, 10 + charge * 7, color, h.Age * .04f, 2, 1, 6);
            if (h.Kind == DroneShot.TrailBomb)
            {
                Brackets(p.Center, new Vector2(25), color, .9f);
                Line(p.Center - new Vector2(0, 8), p.Center + new Vector2(0, 5), White, 3);
            }
            if (h.Age < h.Delay)
                Ring(p.Center, h.MineRadius * (1.6f - .6f * charge), color * .35f, 0, 1);
            else Glow(p.Center, color * (.7f * fade), new Vector2(h.MineRadius * 2));
            for (int i = 0; i < 4; i++)
            {
                Vector2 d = (i * MathHelper.PiOver2).ToRotationVector2();
                Line(p.Center + d * 8, p.Center + d * 22, White * fade, 2);
            }
            return true;
        }
        if (h.Kind == DroneShot.RailSlug)
        {
            if (h.Age < h.Delay)
            {
                Vector2 end = p.Center + axis * 832;
                Line(p.Center, end, color * .6f, 2);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 a = p.Center + axis * (50 + i * 100);
                    Line(a - axis * 10 - n * 9, a, color * .7f, 2);
                    Line(a - axis * 10 + n * 9, a, color * .7f, 2);
                }
                Brackets(p.Center, new Vector2(30), color, .8f);
            }
            else
            {
                // Vanilla high-velocity bullet art; custom hostile AI keeps warning and ownership rules.
                Main.instance.LoadProjectile(ProjectileID.BulletHighVelocity);
                Texture2D texture = TextureAssets.Projectile[ProjectileID.BulletHighVelocity].Value;
                Main.spriteBatch.Draw(texture, p.Center - Main.screenPosition, null, Color.White * h.Visibility, p.rotation + MathHelper.PiOver2,
                    texture.Size() * .5f, 1.2f, SpriteEffects.None, 0);
                Line(p.Center - axis * 36, p.Center, color with { A = 0 }, 3);
            }
            return true;
        }
        if (h.Kind is not (DroneShot.Prism or DroneShot.Seeker)) return false;
        if (h.Age < h.Delay)
        {
            Brackets(p.Center, new Vector2(28), color, .7f);
            if (h.Kind == DroneShot.Prism)
                Line(p.Center, p.Center + axis * 700, color * .35f, 1);
            Ring(p.Center, 18, color * .7f, -h.Age * .04f, 2, 1, 6);
            return true;
        }
        for (int i = p.oldPos.Length - 1; i >= 1; i--)
            if (p.oldPos[i] != Vector2.Zero && p.oldPos[i - 1] != Vector2.Zero)
            {
                float fade = 1 - i / (float)p.oldPos.Length;
                Color trail = Color.Lerp(color, Cyan, i / (float)p.oldPos.Length);
                Line(p.oldPos[i] + p.Size * .5f, p.oldPos[i - 1] + p.Size * .5f, (trail with { A = 0 }) * fade, 4 * fade);
            }
        if (h.Kind == DroneShot.Prism)
        {
            Vector2 front = p.Center + axis * 15, back = p.Center - axis * 12;
            Line(front, p.Center + n * 6, color, 3);
            Line(front, p.Center - n * 6, color, 3);
            Line(back, p.Center + n * 6, Cyan, 2);
            Line(back, p.Center - n * 6, Cyan, 2);
            Line(back, front, White with { A = 0 }, 2);
            Glow(p.Center, color * .55f, new Vector2(38, 20), p.rotation);
        }
        else if (h.Kind == DroneShot.Seeker)
        {
            Glow(p.Center, color * .7f, new Vector2(32));
            Ring(p.Center, 9, color, h.Age * .08f, 2, .7f, 12);
            Ring(p.Center, 14, Cyan * .6f, -h.Age * .05f, 1, .7f, 12);
            Glow(p.Center, White * .9f, new Vector2(9));
        }
        return true;
    }
}
