using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Passenger;

internal static class ChainArtsVfx
{
    internal const int TargetCount = 4; // initial collision + three hops, shared by all three staves

    internal static void Head(Projectile p, LightningPalette palette, int tier)
    {
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        OperatorFlightVfx.Trail(p, palette.Body, palette.Edge, 4f + tier * 3f, 65f + tier * 48f);
        float angle = aim.ToRotation(), time = Main.GlobalTimeWrappedHourly;
        if (tier == 1)
            OperatorFlightVfx.Sprite("StarCore", p.Center, new Vector2(18f), OperatorFlightVfx.Add(palette.Edge, .7f), time * 3f);
        else
        {
            OperatorFlightVfx.Sprite("ForwardFlame", p.Center - aim * 17f,
                new Vector2(18f + tier * 6f, 46f + tier * 15f), OperatorFlightVfx.Add(palette.Body, .5f), angle + MathHelper.PiOver2);
            OperatorFlightVfx.Sprite("OrbitSmear", p.Center, new Vector2(tier == 2 ? 29f : 43f),
                OperatorFlightVfx.Add(palette.Edge, .75f), time * (tier == 2 ? -7f : 9f));
            if (tier == 3)
                OperatorFlightVfx.Sprite("RuptureRing", p.Center - aim * 9f, new Vector2(42f, 23f),
                    OperatorFlightVfx.Add(palette.Body, .65f), angle);
        }
        OperatorFlightVfx.Sprite("LinearSpark", p.Center + aim * 5f, new Vector2(3f + tier, 16f + tier * 5f),
            OperatorFlightVfx.Add(palette.Core, .85f), angle + MathHelper.PiOver2);
    }

    internal static void Flight(Vector2 start, Vector2 end, float scale, int phase, int seed, LightningPalette palette, int tier)
    {
        if (Main.dedServ) return;
        Vector2 aim = (end - start).SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        PassengerLightningVisuals.Fracture(start, end, scale * (tier == 2 ? .75f : 1.2f), seed, .5f, tier == 2 ? 7 : 10, palette);
        if (tier == 2)
        {
            // Two alternating rails merge at the white lightning tip.
            float offset = MathF.Sin(phase * .8f) * 9f;
            PassengerLightningVisuals.Fracture(start + side * offset, end, scale * .45f, seed + 91, .35f, 6, palette);
        }
        else
        {
            // Passenger carries a thicker leader and short, irregular outward branches.
            for (int i = -1; i <= 1; i += 2)
                PassengerLightningVisuals.Fracture(Vector2.Lerp(start, end, .55f), start + side * i * (18f + 6f * MathF.Sin(phase)),
                    scale * .55f, seed + i * 37, .6f, 8, palette);
        }
        for (int i = 0; i < tier; i++)
            OperatorFlightVfx.Spark(Vector2.Lerp(start, end, (i + .5f) / tier) + side * Main.rand.NextFloat(-5f, 5f),
                -aim * Main.rand.NextFloat(2f, 5f), i == 0 ? palette.Core : palette.Edge, 13f + tier * 4f, 10 + tier * 2, 2f);
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
    }
}
