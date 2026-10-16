using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Passenger;

internal static class ChainArtsVfx
{
    internal const int TargetCount = 4; // initial collision + three hops, shared by all three staves

    internal static void Flight(Vector2 start, Vector2 end, float scale, int phase, int seed, LightningPalette palette, int tier)
    {
        if (Main.dedServ) return;
        // Two spatial emission steps per frame: twice the previous discharge cadence.
        // Each step has independent roots, reach and full-circle directions.
        for (int step = 0; step < 2; step++)
        {
            Vector2 a = Vector2.Lerp(start, end, step * .5f);
            Vector2 b = Vector2.Lerp(start, end, (step + 1) * .5f);
            PassengerLightningVisuals.Fracture(a, b, scale * .75f,
                seed + step * 193, .25f, 7, palette);
            for (int fork = 0; fork < 2; fork++)
            {
                Vector2 root = Vector2.Lerp(a, b, Main.rand.NextFloat());
                Vector2 ray = Main.rand.NextVector2Unit();
                PassengerLightningVisuals.Fracture(root, root + ray * Main.rand.NextFloat(12f, 29f) * scale,
                    scale * Main.rand.NextFloat(.32f, .52f), Main.rand.Next(), .25f, 7, palette);
            }
            for (int i = 0; i < 3 && ParticleManager.activeParticles.Count < 1800; i++)
            {
                Vector2 ray = Main.rand.NextVector2Unit();
                Vector2 point = Vector2.Lerp(a, b, Main.rand.NextFloat()) + ray * Main.rand.NextFloat(4f, 11f);
                new T_Chain_Bolt_Particle(point, ray * Main.rand.NextFloat(2.5f, 5.5f),
                    palette.Edge, Main.rand.NextFloat(15f, 24f) * scale, Main.rand.Next(12, 17)).Spawn();
            }
            for (int i = 0; i < 2; i++)
                PassengerLightningVisuals.ColoredDust(Vector2.Lerp(a, b, Main.rand.NextFloat()),
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f), palette.Edge,
                    Main.rand.NextFloat(.8f, 1.2f));
        }
    }

    internal static void Discharge(Vector2 start, Vector2 end, float intensity, LightningPalette palette)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1800) return;
        float length = Vector2.Distance(start, end);
        if (!float.IsFinite(length) || length < 2f || length > 2400f) return;
        new T_Chain_Beam_Particle(start, end, palette.Edge, intensity).Spawn();
        Vector2 normal = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        int count = Math.Clamp((int)(length / 65f), 1, 8);
        for (int i = 0; i < count; i++)
            new T_Chain_Bolt_Particle(Vector2.Lerp(start, end, (i + .5f) / count),
                normal * Main.rand.NextFloat(-2f, 2f), palette.Edge, 18f * intensity, 8).Spawn();
    }

    internal static void Hop(Vector2 start, Vector2 end, float scale, float intensity, int seed, int tier, LightningPalette palette, bool target)
    {
        if (Main.dedServ) return;
        PassengerLightningVisuals.Fracture(start, end, scale * (tier == 2 ? .85f : 1.3f), seed, tier == 2 ? 2f : 3.5f, tier == 2 ? 16 : 22, palette);
        Vector2 side = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        if (tier == 2)
            PassengerLightningVisuals.Fracture(start + side * 7f, end, scale * .42f, seed + 83, 1.2f, 12, palette);
        else
        {
            for (int i = 1; i <= 3; i++)
            {
                Vector2 root = Vector2.Lerp(start, end, i / 4f);
                Vector2 tip = root + side * (i % 2 == 0 ? -1f : 1f) * (24f + i * 7f);
                PassengerLightningVisuals.Fracture(root, tip, scale * .55f, seed + i * 67, 1.4f, 14, palette);
            }
        }
        if (target) OperatorFlightVfx.Ring(end, palette.Edge, tier == 2 ? 38f : 65f, 16);
    }

    internal static void Impact(Vector2 center, float intensity, int tier, LightningPalette palette)
    {
        if (Main.dedServ) return;
        OperatorFlightVfx.Ring(center, palette.Body, (tier == 2 ? 65f : 100f) * intensity, 20);
        int count = tier == 2 ? 7 : 13;
        for (int i = 0; i < count; i++)
        {
            Vector2 ray = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.12f, .12f)).ToRotationVector2();
            OperatorFlightVfx.Spark(center + ray * 5f, ray * Main.rand.NextFloat(2f, tier == 2 ? 5f : 8f),
                i % 3 == 0 ? palette.Core : palette.Edge, tier == 2 ? 18f : 28f, 16, tier == 2 ? 2f : 3f);
        }
        for (int i = 0; i < 8 && ParticleManager.activeParticles.Count < 1800; i++)
        {
            float angle = MathHelper.TwoPi * (i + Main.rand.NextFloat(-.2f, .2f)) / 8f;
            Vector2 ray = angle.ToRotationVector2();
            PassengerLightningVisuals.Fracture(center + ray * 6f,
                center + ray * Main.rand.NextFloat(45f, 95f) * intensity,
                intensity * .6f, Main.rand.Next(), .5f, 11, palette);
            new T_Chain_Bolt_Particle(center + ray * 9f, ray * Main.rand.NextFloat(3f, 7f),
                palette.Edge, Main.rand.NextFloat(17f, 27f) * intensity, 13).Spawn();
        }
    }
}
