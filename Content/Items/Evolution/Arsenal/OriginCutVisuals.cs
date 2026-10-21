using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

internal static class OriginCutVisuals
{
    internal static float Envelope(float progress) => Math.Clamp(progress / .055f, 0, 1) *
        (1 - MathHelper.SmoothStep(0, 1, Math.Clamp((progress - .58f) / .42f, 0, 1)));
    internal static Vector2 CutDirection(OriginSwing swing)
    {
        if (swing.Stage == 2) return swing.Aim;
        float previous = Math.Max(0, swing.Progress - 1 / Math.Max(1, swing.Projectile.ai[2]));
        return (swing.Tip(swing.Progress) - swing.Tip(previous)).SafeNormalize(swing.Aim);
    }
    internal static void Shed(OriginSwing swing, float previous)
    {
        if (Main.dedServ || swing.Progress > .63f || swing.Progress < .06f) return;
        // A small, fast cutting wake between hits; the large spray is reserved for actual contact.
        int count = swing.Stage == 2 ? 3 : 2;
        Vector2 direction = CutDirection(swing);
        for (int i = 0; i < count; i++)
        {
            float time = MathHelper.Lerp(previous, swing.Progress, (i + 1f) / count);
            Vector2 position = Vector2.Lerp(swing.Projectile.Center, swing.Tip(time), Main.rand.NextFloat(.72f, 1));
            Vector2 velocity = direction.RotatedByRandom(.32f) * Main.rand.NextFloat(15, 30) + swing.Aim * 3;
            velocity *= EvolutionArsenalRules.OriginScale;
            if (i == 0)
                new T_Centurion_Blood_Spark_Particle(position, velocity, Main.rand.Next(12, 18),
                    Main.rand.NextFloat(.38f, .65f) * EvolutionArsenalRules.OriginScale, EvolutionWeaponVisuals.Blood * .8f).Spawn();
            else
                new T_Centurion_Blood_Line_Particle(position, velocity, Main.rand.Next(10, 15),
                    Main.rand.NextFloat(.65f, .95f) * EvolutionArsenalRules.OriginScale, new Color(255, 70, 95)).Spawn();
        }
    }
}
