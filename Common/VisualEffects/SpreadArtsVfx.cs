using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Common.VisualEffects;

internal enum SpreadArt { Crystal, Rupture, Meteor }

internal static class SpreadArtsVfx
{
    private static Color Body(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(137, 87, 208), SpreadArt.Rupture => new(28, 145, 183), _ => new(238, 67, 19) };
    private static Color Edge(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(229, 204, 255), SpreadArt.Rupture => new(194, 252, 239), _ => new(255, 216, 114) };
    private static Color Dark(SpreadArt art) => art switch {
        SpreadArt.Crystal => new(23, 13, 47), SpreadArt.Rupture => new(8, 24, 38), _ => new(65, 22, 19) };

    // Helium's sequence: inward streaks -> growing animated core -> full-charge cue -> directional release.
    // Each school has its own silhouette; the shared head bar/one-shot ready feedback stays in the holdout.
    internal static void Charge(Vector2 center, float charge, float time, SpreadArt art)
    {
        using var material = SpreadArtsMaterial.Begin();
        charge = MathHelper.Clamp(charge, 0f, 1f);
        if (charge < .01f) return;
        float size = 12f + 23f * MathHelper.SmoothStep(0f, 1f, charge);
        Core(center, Vector2.UnitX, size, time, art, true);
        int count = art == SpreadArt.Crystal ? 3 : art == SpreadArt.Rupture ? 2 : 4;
        for (int i = 0; i < count; i++)
        {
            float phase = (time * (.6f + charge * .5f) + i / (float)count) % 1f;
            float angle = i * MathHelper.TwoPi / count + time * .9f;
            Vector2 direction = angle.ToRotationVector2();
            float radius = size * .5f + (1f - phase) * (24f + 18f * charge);
            float alpha = MathF.Sin(phase * MathHelper.Pi) * (.3f + .4f * charge);
            SpreadArtsMaterial.Sprite("LinearSpark", center + direction * radius,
                new Vector2(2.5f, 7f + 9f * phase), OperatorFlightVfx.Add(Edge(art), alpha), angle + MathHelper.PiOver2);
        }
    }

    private static void Core(Vector2 center, Vector2 aim, float size, float time, SpreadArt art, bool charging)
    {
        Color body = Body(art), edge = Edge(art);
        float angle = aim.ToRotation();
        if (art == SpreadArt.Crystal)
        {
            // Stratus-inspired glass vortex: dark recess, moving facets, orbital refraction; no opaque disk.
            SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(size * .7f), Dark(art), -time * 1.4f);
            SpreadArtsMaterial.Sprite("CrystalVortex", center, new Vector2(size * 1.65f), body * .75f, time * 1.15f);
            SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * 1.26f, size * .7f), OperatorFlightVfx.Add(edge, .56f), -time * 2.1f);
            SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(size * .5f), OperatorFlightVfx.Add(edge, .85f), -time);
        }
        else if (art == SpreadArt.Rupture)
        {
            // Compressed void with a broken bright shell, matching the Despoiler's dark/bright separation.
            SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(size), Dark(art), time * 2f);
            SpreadArtsMaterial.Sprite("RuptureRing", center, new Vector2(size * 1.25f), OperatorFlightVfx.Add(body, .9f), time * 3.6f);
            SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * 1.1f, size * .55f), OperatorFlightVfx.Add(edge, .75f), -time * 2.8f);
            SpreadArtsMaterial.Sprite("LinearSpark", center + aim * size * .23f, new Vector2(4f, size * .42f), OperatorFlightVfx.Add(edge, .9f), angle + MathHelper.PiOver2);
        }
        else
        {
            // Flame lobes flow rearward and breathe out of phase. Never tint a large circular backing plate.
            if (!charging)
            {
                for (int i = 0; i < 3; i++)
                {
                    float flutter = MathF.Sin(time * 18f + i * 2.1f);
                    float length = size * (2.2f + .4f * flutter - i * .32f);
                    Vector2 at = center - aim * length * .27f;
                    SpreadArtsMaterial.Sprite("ForwardFlame", at, new Vector2(size * (1f - i * .15f), length),
                        OperatorFlightVfx.Add(i == 2 ? edge : body, i == 2 ? .62f : .42f), angle + MathHelper.PiOver2 + flutter * .04f);
                }
            }
            SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * 1.18f), OperatorFlightVfx.Add(body, .7f), time * 4f);
            SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(size * (.68f + .055f * MathF.Sin(time * 22f))), OperatorFlightVfx.Add(edge, .9f), -time * 2.3f);
            SpreadArtsMaterial.Sprite("StarCore", center + aim * size * .08f, new Vector2(size * .28f), OperatorFlightVfx.Add(new Color(255, 250, 210)), time * 1.4f);
        }
    }

    internal static void Flight(Projectile p, SpreadArt art, float power = 1f)
    {
        using var material = SpreadArtsMaterial.Begin();
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        float time = Main.GlobalTimeWrappedHourly;
        float size = (art == SpreadArt.Crystal ? 30f : art == SpreadArt.Rupture ? 28f : 38f) * power;
        float tail = art == SpreadArt.Crystal ? 145f : art == SpreadArt.Rupture ? 235f : 300f;
        OperatorFlightVfx.Trail(p, Body(art), Edge(art), size * .52f, tail);
        if (art != SpreadArt.Meteor)
            SpreadArtsMaterial.Sprite("ForwardFlame", p.Center - aim * size * .75f,
                new Vector2(size * .85f, size * 2.4f), OperatorFlightVfx.Add(Body(art), .36f), aim.ToRotation() + MathHelper.PiOver2);
        Core(p.Center, aim, size, time, art, false);
        if (art == SpreadArt.Rupture)
        {
            Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 7; i++)
            {
                float phase = time * 16f - i * .8f;
                for (int sign = -1; sign <= 1; sign += 2)
                    SpreadArtsMaterial.Sprite("LinearSpark", p.Center - aim * (i * 10f + 6f) + side * MathF.Sin(phase) * (11f - i) * sign,
                        new Vector2(2f, 12f), OperatorFlightVfx.Add(sign > 0 ? Edge(art) : Body(art), (1f - i / 8f) * .7f), aim.ToRotation() + MathHelper.PiOver2);
            }
        }
    }

    internal static void Gather(Vector2 center, float charge, SpreadArt art)
    {
        if (Main.dedServ) return;
        Vector2 outward = Main.rand.NextVector2Unit();
        float distance = Main.rand.NextFloat(28f, 62f);
        int life = Main.rand.Next(10, 16);
        // Inward particles expire before crossing the core, so a held charge never sprays outward.
        OperatorFlightVfx.Spark(center + outward * distance, -outward * (distance / life * 1.16f),
            Main.rand.NextBool(3) ? Edge(art) : Body(art), 9f + charge * 10f, life, 2f);
        if (Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustPerfect(center + outward * distance, DustID.TintableDustLighted, -outward * 1.6f, 150, Body(art), .6f);
            dust.noGravity = true;
        }
    }

    internal static void Wake(Vector2 center, Vector2 velocity, SpreadArt art, int phase = 0)
    {
        if (Main.dedServ) return;
        Vector2 aim = velocity.SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        int count = art == SpreadArt.Crystal ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            float lateral = art == SpreadArt.Rupture ? MathF.Sin(phase * .45f + i * MathHelper.Pi) * 11f : Main.rand.NextFloat(-9f, 9f);
            Vector2 point = center - aim * Main.rand.NextFloat(4f, 18f) + side * lateral;
            OperatorFlightVfx.Spark(point, -aim * Main.rand.NextFloat(2f, 5f) + side * lateral * .035f,
                i == 0 ? Edge(art) : Body(art), Main.rand.NextFloat(12f, 29f), Main.rand.Next(12, 22), art == SpreadArt.Meteor ? 4f : 2.5f);
            if (art == SpreadArt.Meteor && i == 1)
                OperatorFlightVfx.Flame(point, -aim * Main.rand.NextFloat(1f, 3f), Body(art), Edge(art), 24f, 17);
        }
    }

    internal static void Release(Vector2 center, Vector2 direction, SpreadArt art)
    {
        if (Main.dedServ) return;
        direction = direction.SafeNormalize(Vector2.UnitX);
        OperatorFlightVfx.Ring(center, Body(art), 42f, 13);
        for (int i = 0; i < 10; i++)
            OperatorFlightVfx.Spark(center, direction.RotatedByRandom(.38f) * Main.rand.NextFloat(3f, 8f),
                i % 3 == 0 ? Edge(art) : Body(art), 15f + i, 15, 2.5f);
    }

    internal static void Burst(Vector2 center, float diameter, float progress, SpreadArt art)
    {
        using var material = SpreadArtsMaterial.Begin();
        progress = MathHelper.Clamp(progress, 0f, 1f);
        diameter = MathHelper.Clamp(diameter, 60f, 520f);
        if (art == SpreadArt.Meteor)
        {
            MeteorBurst(center, diameter, progress);
            return;
        }
        float opening = 1f - MathF.Pow(1f - progress, 3f), fade = MathF.Pow(1f - progress, 1.7f);
        float flash = MathHelper.Clamp(1f - progress * 5f, 0f, 1f);
        float size = diameter * (.28f + .8f * opening);
        float coreFade = MathF.Pow(1f - progress, 2.3f);
        SpreadArtsMaterial.Sprite("BlastBloom", center, new Vector2(diameter * (.16f + .36f * opening)),
            OperatorFlightVfx.Add(Body(art), coreFade * .82f));
        SpreadArtsMaterial.Sprite("BlastBloom", center, new Vector2(diameter * (.09f + .19f * opening)),
            OperatorFlightVfx.Add(Edge(art), coreFade * .95f));
        // Independent dark volume, textured wavefront and fragmented outer crown.
        SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(size * .95f), Dark(art) * (fade * .78f), progress * .15f);
        if (art == SpreadArt.Crystal)
        {
            SpreadArtsMaterial.Sprite("CrystalVortex", center, new Vector2(size), Body(art) * (fade * .75f), progress * .65f);
            SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * .85f, size * .4f), OperatorFlightVfx.Add(Edge(art), fade * .38f), -progress);
        }
        else
        {
            int lobes = art == SpreadArt.Rupture ? 6 : 8;
            for (int i = 0; i < lobes; i++)
            {
                float angle = i * MathHelper.TwoPi / lobes + .2f;
                Vector2 offset = angle.ToRotationVector2() * size * (.15f + .07f * MathF.Sin(i * 7f));
                SpreadArtsMaterial.Sprite("ForwardFlame", center + offset, new Vector2(size * .22f, size * .42f),
                    OperatorFlightVfx.Add(i % 3 == 0 ? Edge(art) : Body(art), fade * .48f), angle + MathHelper.PiOver2);
            }
        }
        SpreadArtsMaterial.Sprite("RuptureRing", center, new Vector2(size * 1.12f), OperatorFlightVfx.Add(Body(art), fade * .32f), progress * .5f);
        SpreadArtsMaterial.Sprite("OrbitSmear", center, new Vector2(size * .72f), OperatorFlightVfx.Add(Edge(art), fade * .38f), -progress * .9f);
        SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(diameter * (.12f + flash * .25f)), OperatorFlightVfx.Add(Edge(art), flash), progress * 3f);
    }

    private static void MeteorBurst(Vector2 center, float diameter, float progress)
    {
        float spread = 1f - MathF.Pow(1f - progress, 3.6f);
        float heat = MathF.Pow(1f - progress, 1.45f);
        float flash = MathF.Pow(MathHelper.Clamp(1f - progress * 4.5f, 0f, 1f), 2f);
        float size = diameter * (.25f + .72f * spread);
        // The fire blast has a rolling hot body and an oblique shock front, unlike the hollow Arts rupture.
        SpreadArtsMaterial.Sprite("ImpactPetals", center - Vector2.UnitY * size * .04f,
            new Vector2(size * 1.8f), new Color(51, 21, 26) * (heat * .8f), progress * .24f);
        SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(size * 1.65f),
            OperatorFlightVfx.Add(new Color(255, 81, 18), heat * .82f), -progress * .17f);
        SpreadArtsMaterial.Sprite("ImpactPetals", center - Vector2.UnitY * size * .04f, new Vector2(size * 1.18f),
            OperatorFlightVfx.Add(new Color(255, 191, 78), heat * .72f), progress * .32f);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * MathHelper.TwoPi / 7f + .3f;
            Vector2 offset = angle.ToRotationVector2() * size * (.25f + .04f * MathF.Sin(i * 13f));
            SpreadArtsMaterial.Sprite("ForwardFlame", center + offset, new Vector2(size * .16f, size * (.4f + i % 3 * .06f)),
                OperatorFlightVfx.Add(i % 2 == 0 ? new Color(255, 167, 54) : new Color(245, 71, 17), heat * .62f), angle + MathHelper.PiOver2);
        }
        SpreadArtsMaterial.Sprite("RuptureRing", center, new Vector2(size * 1.28f, size * .48f),
            OperatorFlightVfx.Add(new Color(255, 179, 85), heat * .65f), -.15f);
        SpreadArtsMaterial.Sprite("StarCore", center, new Vector2(diameter * (.16f + flash * .25f)),
            OperatorFlightVfx.Add(new Color(255, 250, 214), flash));
    }

    internal static void Debris(Vector2 center, SpreadArt art, float power = 1f)
    {
        if (Main.dedServ) return;
        int count = art == SpreadArt.Crystal ? 20 : art == SpreadArt.Rupture ? 30 : 38;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.13f, .13f);
            Vector2 direction = angle.ToRotationVector2();
            OperatorFlightVfx.Spark(center + direction * 10f, direction * Main.rand.NextFloat(3f, 11f) * power,
                i % 3 == 0 ? Edge(art) : Body(art), Main.rand.NextFloat(18f, 40f), Main.rand.Next(15, 28), i % 3 == 0 ? 4f : 2f);
            if (art == SpreadArt.Meteor && i % 3 == 0)
                OperatorFlightVfx.Flame(center + direction * 12f, direction * Main.rand.NextFloat(3f, 7f),
                    Body(art), Edge(art), Main.rand.NextFloat(20f, 34f), Main.rand.Next(18, 28));
            if (i % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.TintableDustLighted, direction * Main.rand.NextFloat(3f, 8f), 130, Body(art), .9f);
                dust.noGravity = true;
            }
        }
        OperatorFlightVfx.Ring(center, Edge(art), 90f * power);
        OperatorFlightVfx.Ring(center, Body(art), 170f * power, 26);
    }
}
