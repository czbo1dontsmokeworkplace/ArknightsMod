using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    private Vector2 ScorePoint(float time) => EvolutionRecitalRules.SeedPoint(ArsenalOrigin, Projectile.velocity, Parameter, BurstAngle, time);
    private void UpdateScoreSeed(Evolution boss)
    {
        Projectile.Center = ScorePoint(Age - Delay);
        if (Age == Delay) EvolutionPianoVisuals.Strike(Projectile.Center, ScorePitch(boss));
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, EvolutionVisuals.Blood.ToVector3() * .5f);
        if (Age > Delay && Age % 4 == 0)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.Blood,
                -Projectile.velocity * .06f, 100, EvolutionVisuals.Blood, .8f);
            dust.noGravity = true;
        }
    }
    private float ScorePitch(Evolution boss) => Math.Clamp((boss.Arena.Y - Projectile.Center.Y) / EvolutionRules.CorridorHeight, -.5f, .5f);

    private void DrawScoreSeed()
    {
        if (Age < Delay)
        {
            float grow = Math.Clamp(Age / 10f, 0, 1);
            float opacity = grow * (.13f + .07f * Math.Clamp(Age / (float)Delay, 0, 1));
            Vector2 previous = ArsenalOrigin;
            // Telegraph and flight share the same analytical curve, including the first fourteen ticks.
            for (int i = 1; i <= 24; i++)
            {
                Vector2 point = ScorePoint((Lifetime - Delay - 12) * i / 24f);
                EvolutionVisuals.Line(previous, point, EvolutionVisuals.Blood * opacity, 1.2f, false);
                previous = point;
            }
            EvolutionVisuals.Glow(ArsenalOrigin, EvolutionVisuals.Blood * (.35f * grow), new Vector2(19, 13), Projectile.velocity.ToRotation());
            return;
        }
        float time = Age - Delay, fade = Math.Clamp((Lifetime - Age) / 12f, 0, 1);
        for (int i = 12; i >= 1; i--)
        {
            if (time < i * 1.4f) continue;
            float strength = (1 - i / 13f) * fade;
            EvolutionVisuals.Line(ScorePoint(time - i * 1.4f), ScorePoint(time - (i - 1) * 1.4f),
                EvolutionVisuals.Blood * (.6f * strength), 1 + strength * 6, false);
        }
        Vector2 direction = (ScorePoint(time + 1) - ScorePoint(time)).SafeNormalize(Vector2.UnitX);
        float angle = direction.ToRotation();
        EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * (.6f * fade), new Vector2(25, 13), angle);
        EvolutionVisuals.Glow(Projectile.Center, new Color(255, 215, 223) * (.9f * fade), new Vector2(13, 6), angle);
        EvolutionVisuals.Line(Projectile.Center - direction * 4, Projectile.Center + direction * 5,
            new Color(255, 215, 223) * (.85f * fade), 1.3f, false);
        // Paired red filaments are decorative; only the compact centre and its swept path deal damage.
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 mid = Projectile.Center - direction * 9 + normal * (side * (7 + MathF.Sin(time * .18f) * 2));
            EvolutionVisuals.Line(Projectile.Center + direction * 7, mid, EvolutionVisuals.Blood * (.7f * fade), 1.6f, false);
            EvolutionVisuals.Line(mid, Projectile.Center - direction * 18, EvolutionVisuals.Blood * (.35f * fade), 1, false);
        }
    }
}
