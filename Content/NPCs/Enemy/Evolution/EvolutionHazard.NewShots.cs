using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    internal float BombRadius => MathHelper.Clamp(Math.Abs(Parameter), 90, 210);
    private void UpdateCrimsonBomb(Evolution boss)
    {
        Projectile.velocity *= .955f;
        // The final warning is stationary, so its blast disc never chases a dodging player.
        if (Age >= Delay - 36) Projectile.velocity = Vector2.Zero;
        if (Age != FireAge) return;
        EvolutionImpactSystem.Emit(Projectile.Center, BombRadius * 1.8f, 4.5f);
        EvolutionVisuals.Burst(Projectile.Center, 1.6f, false);
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -.55f, Volume = .55f, MaxInstances = 3 }, Projectile.Center);
        if (Parameter < 0)
            for (int i = 0; i < 12; i++) boss.Shoot(EvolutionShot.Fragment, Projectile.Center,
                (i * MathHelper.TwoPi / 12).ToRotationVector2() * 7.5f, lifetime: 125);
    }
    private void DrawCrimsonBomb()
    {
        Vector2 center = Projectile.Center;
        Color wine = new(148, 9, 43), rim = new(255, 83, 98);
        if (Age < FireAge)
        {
            float charge = MathHelper.Clamp((Age - Delay + 36) / 36f, 0, 1);
            float breathe = 1 + MathF.Sin(Age * .2f) * .06f;
            EvolutionVisuals.Glow(center, wine * .8f, new Vector2(105 * breathe));
            Texture2D shell = EvolutionVisuals.Asset("BloodRock");
            Main.spriteBatch.Draw(shell, center - Main.screenPosition, null, new Color(153, 43, 64), Age * .025f,
                shell.Size() * .5f, 48f * breathe / Math.Max(shell.Width, shell.Height), SpriteEffects.None, 0);
            EvolutionVisuals.Glow(center, rim * (.3f + charge * .5f), new Vector2(26 + charge * 12));
            if (Age >= Delay - 36 && Age < Delay)
            {
                if (Parameter >= 0)
                {
                    EvolutionVisuals.Glow(center, wine * .15f, new Vector2(BombRadius * 2));
                    EvolutionVisuals.Ring(center, BombRadius, 0, rim * (.3f + charge * .55f), 2, false);
                }
                else
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 direction = (i * MathHelper.TwoPi / 12).ToRotationVector2();
                        EvolutionVisuals.Line(center + direction * 30, center + direction * (60 + charge * 60), rim * .6f, 1.5f);
                    }
            }
        }
        else
        {
            float age = Age - FireAge;
            float fade = 1 - MathHelper.Clamp(age / 28f, 0, 1);
            float radius = Parameter >= 0 ? BombRadius : 90;
            // The damaging disc stays fully drawn for all eight damage ticks.
            EvolutionVisuals.Glow(center, wine * (fade * .85f), new Vector2(radius * 2.2f));
            EvolutionVisuals.Glow(center, EvolutionVisuals.Core * (fade * .45f), new Vector2(radius * 1.5f));
            EvolutionVisuals.Ring(center, radius, 0, rim * fade, age < 8 ? 6 : 3, false);
        }
    }
    private void DrawEruption()
    {
        Vector2 center = Projectile.Center, direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
        float length = Parameter > 0 ? Parameter : 360;
        if (Age < Delay)
        {
            EvolutionVisuals.AimLine(center, center + direction * length, (Age + 1f) / Math.Max(1, Delay));
            EvolutionVisuals.Glow(center, EvolutionVisuals.Blood * .55f, new Vector2(100, 22));
            for (int i = -3; i < 3; i++) EvolutionVisuals.Line(center + new Vector2(i * 15, i % 2 * 4),
                center + new Vector2((i + 1) * 15, (i + 1) % 2 * 4), EvolutionVisuals.Core * .7f, 2);
            return;
        }
        if (Age < FireAge) return;
        float fade = MathHelper.Clamp((Lifetime - Age) / 20f, 0, 1);
        float reach = Math.Min(1, (Age - FireAge + 1) / 6f);
        Vector2 end = center + direction * length * reach;
        EvolutionVisuals.Line(center, end, new Color(52, 0, 19) * fade, 38);
        EvolutionVisuals.Line(center, end, EvolutionVisuals.Blood * fade, 26);
        Texture2D shard = EvolutionVisuals.Asset("ShellFragment");
        for (int i = 0; i < 8; i++)
        {
            float u = (i + .5f) / 8;
            Vector2 point = Vector2.Lerp(center, end, u);
            Main.spriteBatch.Draw(shard, point - Main.screenPosition, null, new Color(226, 111, 115) * fade,
                direction.ToRotation() + MathHelper.PiOver2 + (i % 2 == 0 ? -.3f : .3f), shard.Size() * .5f,
                new Vector2(32, 65) * (1 - u * .3f) / shard.Size(), SpriteEffects.None, 0);
        }
        EvolutionVisuals.Glow(end, EvolutionVisuals.Core * (.7f * fade), new Vector2(44, 80), direction.ToRotation() + MathHelper.PiOver2);
    }
}
