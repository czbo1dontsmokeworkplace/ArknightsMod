using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Common.VisualEffects;

internal enum SpreadArt { Crystal, Rupture, Meteor, Ember }

internal static class SpreadArtsVfx
{
    internal static Color Body(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(149, 91, 223), SpreadArt.Rupture => new(47, 172, 201),
        SpreadArt.Ember => new(237, 89, 35), _ => new(246, 74, 22) };
    internal static Color Edge(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(227, 197, 255), SpreadArt.Rupture => new(174, 240, 234),
        SpreadArt.Ember => new(255, 189, 92), _ => new(255, 216, 114) };
    private static Color Dark(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(33, 18, 59), SpreadArt.Rupture => new(9, 29, 40), _ => new(65, 22, 29) };
    private static bool Fire(SpreadArt art) => art is SpreadArt.Meteor or SpreadArt.Ember;

    internal static void Charge(Vector2 center, float charge, float time, SpreadArt art, Vector2 direction)
    {
        if (!SpreadVfxBudget.Visible(center, 140f)) return;
        Vector2 aim = direction.SafeNormalize(Vector2.UnitX);
        float rotation = aim.ToRotation();
        float late = MathHelper.Clamp((charge - .45f) / .55f, 0f, 1f);
        charge = SpreadChargeVfx.Growth(charge);
        float size = (14f + 23f * charge) * SpreadChargeVfx.ArtScale(art);
        float breathe = 1f + .045f * MathF.Sin(time * 10f);
        // Arc Nova: three nested ellipsoidal blooms with a directional body.
        // Despoiler: saturated corona enclosing a darker compressed center.
        using (SpreadArtsMaterial.Begin())
        {
            for (int i = 0; i < 3; i++)
                SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(1.3f, 1f) * size * (2.2f - i * .48f) * breathe,
                    OperatorFlightVfx.Add(i == 2 ? Edge(art) : Body(art), (.48f + charge * .18f) * (1f - i * .16f)), rotation);
            SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * 1.5f, size * 1.15f),
                OperatorFlightVfx.Add(Body(art), .3f + charge * .2f), rotation + time * 1.4f);
            if (Fire(art)) FlameBody(center, aim, size * .8f, time, art);
            // Four short incoming streams are part of the aimed drawing, while
            // the longer absorption trails below are independent world particles.
            for (int i = 0; i < 4; i++)
            {
                float offset = (time * 1.8f + i * .25f) % 1f;
                float side = (i % 2 == 0 ? -1 : 1) * size * (.35f + .2f * MathF.Sin(i * 1.7f));
                Vector2 p = SpreadChargeVfx.AimPoint(center, aim, -size * (1.8f - offset), side * (1f - offset));
                SpreadArtsMaterial.Sprite("ChargeBloom", p, new Vector2(size * .75f, size * .14f),
                    OperatorFlightVfx.Add(Body(art), MathF.Sin(offset * MathHelper.Pi) * charge * .7f), rotation);
            }
        }
        // A solid center preserves contrast against bright terrain. All dimensions
        // are world pixels and rotate in the same aim basis as the staff/muzzle.
        if (art != SpreadArt.Ember)
            SpreadVfxDetail.Disc(center, size * (.18f + late * .12f), Dark(art) * (.65f + late * .25f));
        if (art == SpreadArt.Crystal)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = i * MathHelper.TwoPi / 3 + time * 1.3f;
                Vector2 p = SpreadChargeVfx.AimPoint(center, aim, MathF.Cos(angle) * size * .38f, MathF.Sin(angle) * size * .68f);
                SpreadVfxDetail.Facet(p, 5f + 5f * charge, 15f + 13f * charge, rotation + MathF.Sin(angle) * .2f, Edge(art), .9f);
            }
        }
        if (art == SpreadArt.Rupture)
            for (int i = 0; i < 3; i++)
                SpreadVfxDetail.Ellipse(SpreadChargeVfx.AimPoint(center, aim, (i - 1) * size * .4f, 0f), aim,
                    new Vector2(size * .24f, size * (.6f + .12f * i)), time + i * 1.8f, 4.6f,
                    OperatorFlightVfx.Add(Edge(art), .65f), 1.6f);
        // Arc Nova's three orbiting nodes make the late stage immediately readable.
        using (SpreadArtsMaterial.Begin())
        {
            if (late > 0f)
                for (int i = 0; i < 3; i++)
                {
                    float angle = time * 3f + i * MathHelper.TwoPi / 3f;
                    Vector2 point = SpreadChargeVfx.AimPoint(center, aim, MathF.Cos(angle) * size * .48f, MathF.Sin(angle) * size * .95f);
                    SpreadArtsMaterial.Sprite("ChargeBloom", point, new Vector2(size * .46f), OperatorFlightVfx.Add(Body(art), late * .9f));
                    SpreadArtsMaterial.Sprite("ChargeBloom", point, new Vector2(size * .18f), OperatorFlightVfx.Add(Edge(art), late));
                }
            SpreadArtsMaterial.Sprite("ChargeBloom", center + aim * size * .12f, new Vector2(size * .42f, size * .27f),
                OperatorFlightVfx.Add(Edge(art), .85f), rotation);
        }
    }

    private static void FlameBody(Vector2 center, Vector2 aim, float size, float time, SpreadArt art)
    {
        // The source flame points up (-Y). Rotate that tip into the flight direction.
        float angle = aim.ToRotation() + MathHelper.PiOver2;
        for (int i = 0; i < 3; i++)
        {
            float flutter = MathF.Sin(time * 16f + i * 2.2f);
            float length = size * (2.3f - i * .45f + flutter * .12f);
            SpreadArtsMaterial.Sprite("ForwardFlame", center - aim * length * .32f,
                new Vector2(size * (1.05f - i * .21f), length),
                OperatorFlightVfx.Add(i == 2 ? Edge(art) : Body(art), i == 2 ? .7f : .52f), angle + flutter * .07f);
        }
        SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(size * .4f), OperatorFlightVfx.Add(Edge(art), .9f));
    }

    internal static void Flight(Projectile p, SpreadArt art, float power = 1f)
    {
        if (!SpreadVfxBudget.Visible(p.Center, 360f)) return;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        float time = Main.GlobalTimeWrappedHourly, angle = aim.ToRotation();
        float size = (art == SpreadArt.Ember ? 20f : art == SpreadArt.Meteor ? 38f : 28f) * power;
        OperatorFlightVfx.Trail(p, Body(art), Edge(art), size * .38f,
            art == SpreadArt.Ember ? 100f : art == SpreadArt.Meteor ? 300f : 190f);
        using (SpreadArtsMaterial.Begin())
        {
            if (Fire(art)) FlameBody(p.Center, aim, size, time, art);
            else
            {
                SpreadArtsMaterial.Sprite("OrbitSmear", p.Center - aim * 8f, new Vector2(size * 1.5f, size * .85f),
                    OperatorFlightVfx.Add(Body(art), .38f), time * 2f);
                SpreadArtsMaterial.Sprite("StarCore", p.Center, new Vector2(size * .65f), Dark(art));
            }
        }
        if (art == SpreadArt.Crystal)
        {
            SpreadVfxDetail.Facet(p.Center, size * .5f, size * 1.35f, angle, Edge(art), 1f);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 flank = p.Center - aim * 19f + side * (11f + 3f * MathF.Sin(time * 12f)) * sign;
                SpreadVfxDetail.Facet(flank, 5f, 15f, angle + sign * .2f, Body(art), .85f);
            }
        }
        else if (art == SpreadArt.Rupture)
        {
            // Three separated pressure fronts, each normal to the actual travel direction.
            for (int i = 0; i < 3; i++)
                SpreadVfxDetail.Arc(p.Center - aim * (i * 17f + 4f), 12f + i * 3f,
                    angle - 1.05f, 2.1f, OperatorFlightVfx.Add(i == 0 ? Edge(art) : Body(art), .85f - i * .22f), 2.4f - i * .45f);
            SpreadVfxDetail.Facet(p.Center, size * .42f, size, angle, new Color(89, 140, 161), 1f);
        }
        else if (art == SpreadArt.Meteor)
            SpreadVfxDetail.Facet(p.Center, size * .56f, size * .7f, angle, new Color(91, 45, 39), 1f);
    }

    internal static void Gather(Vector2 center, float charge, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(center)) return;
        Vector2 ray = Main.rand.NextVector2Unit();
        float distance = Main.rand.NextFloat(22f, 44f);
        if (Fire(art)) SpreadVfxDetail.Fire(center + ray * distance, -ray * 2.6f, 12f + charge * 10f, 14, distance);
        else SpreadVfxDetail.Fragment(center + ray * distance, -ray * 2.8f, Body(art), 9f + 5f * charge, 14, distance);
        if (Main.rand.NextBool(3)) CasterSparkles.Spawn(center + ray * distance * .4f, -ray * .5f, Edge(art), .4f, 10);
    }

    // Call once per real tick, never once per extraUpdate. Interpolated emission points
    // follow the last frame's flight segment; high speed cannot leave a dotted wake.
    internal static void Wake(Vector2 center, Vector2 velocity, SpreadArt art, int phase = 0)
    {
        if (!SpreadVfxBudget.Visible(center, 220f)) return;
        Vector2 aim = velocity.SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        int count = art == SpreadArt.Ember ? 3 : 5;
        float segment = Math.Min(velocity.Length() * 3f, 72f);
        for (int i = 0; i < count; i++)
        {
            float along = (i + .5f) / count;
            float lateral = MathF.Sin(phase * .55f + i * 2.1f) * (art == SpreadArt.Ember ? 4f : 9f);
            Vector2 point = center - aim * segment * along + side * lateral;
            Vector2 drift = -aim * Main.rand.NextFloat(1f, 3.5f) + side * lateral * .14f;
            if (Fire(art))
            {
                SpreadVfxDetail.Fire(point, drift, art == SpreadArt.Ember ? 16f : 27f, 19, 42f);
                if (i == 0) SpreadVfxDetail.Fire(point, drift * .35f, art == SpreadArt.Ember ? 20f : 36f, 28, 35f, true);
            }
            else SpreadVfxDetail.Fragment(point, drift, i % 2 == 0 ? Edge(art) : Body(art),
                art == SpreadArt.Crystal ? 13f : 9f, 20, 44f);
        }
        if (phase % 3 == 0)
        {
            Vector2 root = center - aim * 12f;
            CasterSparkles.Spawn(root + side * 9f, side * .6f - aim, Edge(art), .45f, 10, aim.ToRotation());
            CasterSparkles.Spawn(root - side * 9f, -side * .6f - aim, Body(art), .35f, 10, aim.ToRotation());
        }
        if (phase % 4 == 0)
        {
            Dust dust = Dust.NewDustPerfect(center - aim * 8f, Fire(art) ? DustID.Torch : DustID.TintableDustLighted,
                -aim * 1.8f + side * Main.rand.NextFloat(-1, 1), 110, Body(art), .8f);
            dust.noGravity = true;
        }
    }

    internal static void Release(Vector2 center, Vector2 direction, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(center)) return;
        direction = direction.SafeNormalize(Vector2.UnitX);
        SpreadChargeVfx.Pulse(center, direction, art, true);
        for (int i = 0; i < 9; i++)
        {
            Vector2 ray = direction.RotatedBy((i - 4) * .07f);
            if (Fire(art)) SpreadVfxDetail.Fire(center, ray * Main.rand.NextFloat(3f, 5f), 18f, 15, 48f);
            else SpreadVfxDetail.Fragment(center, ray * Main.rand.NextFloat(3f, 6f), Edge(art), 11f, 17, 55f);
        }
        OperatorFlightVfx.Ring(center, Body(art), 38f, 12);
    }

    internal static void Burst(Vector2 center, float diameter, float progress, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(center, diameter * .6f)) return;
        progress = MathHelper.Clamp(progress, 0f, 1f);
        diameter = MathHelper.Clamp(diameter, 60f, 520f);
        float opening = 1f - MathF.Pow(1f - progress, 4f);
        float fade = MathF.Pow(1f - progress, 1.4f);
        float flash = MathF.Pow(MathHelper.Clamp(1f - progress * 4f, 0, 1), 2f);
        float size = diameter * (.2f + .77f * opening);
        using (SpreadArtsMaterial.Begin())
        {
            // Dark volume -> colored blast face -> short hot center; every layer is
            // explicitly bounded by the same diameter as the projectile collision.
            SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(size), Dark(art) * (fade * .7f), progress * .16f);
            if (Fire(art))
            {
                SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(size * .94f), OperatorFlightVfx.Add(Body(art), fade * .72f), -progress * .25f);
                for (int i = 0; i < (art == SpreadArt.Ember ? 5 : 9); i++)
                {
                    float a = i * 2.39996f + MathF.Sin(i * 7.1f) * .16f;
                    Vector2 ray = a.ToRotationVector2();
                    float lobe = .72f + .28f * MathF.Sin(i * 3.7f + progress * 3f);
                    Vector2 root = center + ray * size * (.13f + i % 3 * .035f) - Vector2.UnitY * size * progress * .07f;
                    SpreadArtsMaterial.Sprite("ForwardFlame", root,
                        new Vector2(size * .25f, size * .51f * lobe), OperatorFlightVfx.Add(i % 3 == 0 ? Edge(art) : Body(art), fade * .67f), a + MathHelper.PiOver2 + progress * .24f);
                }
                SpreadArtsMaterial.Sprite("BlastBloom", center - Vector2.UnitY * size * .03f,
                    new Vector2(size * .39f, size * .29f), OperatorFlightVfx.Add(Body(art), fade * .45f));
            }
            else
            {
                SpreadArtsMaterial.Sprite("CrystalVortex", center, new Vector2(size * .75f), OperatorFlightVfx.Add(Body(art), fade * .27f), progress * .4f);
                SpreadArtsMaterial.Sprite("RuptureRing", center, new Vector2(size), OperatorFlightVfx.Add(Body(art), fade * .5f), -progress * .12f);
            }
            SpreadArtsMaterial.Sprite("BlastBloom", center, new Vector2(diameter * (.10f + flash * .23f)), OperatorFlightVfx.Add(Edge(art), flash * .7f));
        }
        if (art == SpreadArt.Crystal)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathHelper.TwoPi / 8 + .18f;
                Vector2 ray = a.ToRotationVector2();
                SpreadVfxDetail.Facet(center + ray * size * .31f, size * .065f, size * .22f, a + progress * .35f,
                    i % 2 == 0 ? Edge(art) : Body(art), fade);
            }
        }
        else if (art == SpreadArt.Rupture)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathHelper.TwoPi / 6 + .2f;
                Vector2 ray = a.ToRotationVector2(), side = ray.RotatedBy(MathHelper.PiOver2);
                Vector2 root = center + ray * size * .10f;
                Vector2 joint = center + ray * size * .29f + side * size * .035f;
                Vector2 end = center + ray * size * .46f;
                SpreadVfxDetail.Segment(root, joint, OperatorFlightVfx.Add(Body(art), fade * .75f), 3f * fade);
                SpreadVfxDetail.Segment(joint, end, OperatorFlightVfx.Add(Edge(art), fade * .8f), 1.5f);
                SpreadVfxDetail.Arc(center, size * .44f, a, .62f, OperatorFlightVfx.Add(Edge(art), fade * .65f), 1.8f);
            }
        }
    }

    internal static void Debris(Vector2 center, SpreadArt art, float radius)
    {
        if (!SpreadVfxBudget.Visible(center, radius + 60f)) return;
        int count = art == SpreadArt.Ember ? 16 : art == SpreadArt.Meteor ? 36 : 24;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.06f, .06f);
            Vector2 ray = a.ToRotationVector2();
            float speed = radius * Main.rand.NextFloat(.035f, .065f);
            Vector2 root = center + ray * radius * .1f;
            if (Fire(art))
            {
                SpreadVfxDetail.Fire(root, ray * speed, Math.Min(54f, radius * .26f), 25, radius * .7f);
                if (i % 3 == 0) SpreadVfxDetail.Fire(root, ray * speed * .35f, Math.Min(66f, radius * .32f), 35, radius * .55f, true);
                if (i % 2 == 0) SpreadVfxDetail.Fragment(root, ray * speed, new Color(203, 101, 58), 10f, 30, radius * .75f, true);
            }
            else SpreadVfxDetail.Fragment(root, ray * speed, i % 3 == 0 ? Edge(art) : Body(art),
                Main.rand.NextFloat(11f, 24f), 28, radius * .78f);
            if (i % 4 == 0) CasterSparkles.Spawn(center + ray * radius * .4f, ray * 1.2f, Edge(art), .65f, 13, a);
            if (i % 5 == 0)
            {
                Dust dust = Dust.NewDustPerfect(root, Fire(art) ? DustID.Torch : DustID.TintableDustLighted,
                    ray * Math.Min(5f, speed), 110, Body(art), 1f);
                dust.noGravity = true;
            }
        }
    }
}
