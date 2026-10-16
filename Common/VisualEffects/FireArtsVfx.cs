using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Common.VisualEffects;

// Composition references: Calamity's DrizzlefishFire (small head + Dust),
// PristineFire (opposed sine streams), ApoctosisBlast (soft core + fine sparks).
// Independent implementation using only local assets and Terraria particles.
internal static class FireArtsVfx
{
    private static bool Large(SpreadArt art) => art == SpreadArt.Meteor;
    private static void Glow(Vector2 p, Vector2 size, Color color, float opacity, float rotation = 0f)
        => SpreadArtsMaterial.Sprite("ChargeBloom", p, size, OperatorFlightVfx.Add(color, opacity), rotation);

    private static void DustFire(Vector2 p, Vector2 velocity, int index, float scale)
    {
        if (!SpreadVfxBudget.Take(p, 280f)) return;
        // Keep the lava specks small: their texture is granular, not a light bloom.
        bool lava = index % 5 == 0;
        Dust d = Dust.NewDustPerfect(p, lava ? DustID.Lava : DustID.Torch, velocity,
            35, default, scale * (lava ? .65f : 1f));
        d.noGravity = true;
    }

    internal static void Charge(Vector2 p, float charge, float time, SpreadArt art, Vector2 direction)
    {
        if (!SpreadVfxBudget.Visible(p, 120f)) return;
        float size = (13f + SpreadChargeVfx.Growth(charge) * 17f) * (Large(art) ? 1.2f : .8f);
        Vector2 aim = direction.SafeNormalize(Vector2.UnitX);
        using (SpreadArtsMaterial.Begin())
        {
            Glow(p, new Vector2(size * 2.6f, size * 1.6f), SpreadArtsVfx.Body(art), .65f, aim.ToRotation());
            Glow(p + aim * 3f, new Vector2(size, size * .65f), SpreadArtsVfx.Edge(art), .9f, aim.ToRotation());
            for (int i = 0; i < 2; i++)
            {
                float phase = (time * 1.7f + i * .5f) % 1f;
                Vector2 pos = SpreadChargeVfx.AimPoint(p, aim, -size * (1.5f - phase),
                    MathF.Sin(phase * MathHelper.Pi) * size * .5f * (i == 0 ? -1 : 1));
                Glow(pos, new Vector2(size * .7f, size * .16f), SpreadArtsVfx.Body(art),
                    MathF.Sin(phase * MathHelper.Pi) * .65f, aim.ToRotation());
            }
        }
    }

    internal static void Flight(Projectile p, SpreadArt art, float power)
    {
        if (!SpreadVfxBudget.Visible(p.Center, 240f)) return;
        float size = (Large(art) ? 30f : 15f) * MathHelper.Clamp(power, .5f, 1.5f);
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        OperatorFlightVfx.Trail(p, SpreadArtsVfx.Body(art), SpreadArtsVfx.Edge(art), size * .24f, Large(art) ? 210f : 95f);
        using (SpreadArtsMaterial.Begin())
        {
            Glow(p.Center - aim * size * .4f, new Vector2(size * 2.7f, size * 1.3f), SpreadArtsVfx.Body(art), .8f, aim.ToRotation());
            Glow(p.Center, new Vector2(size * 1.1f, size * .65f), SpreadArtsVfx.Edge(art), 1f, aim.ToRotation());
        }
    }

    internal static void Gather(Vector2 p, float charge, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(p, 120f)) return;
        Vector2 ray = Main.rand.NextVector2Unit();
        DustFire(p + ray * (22f + charge * 16f), -ray * 2.5f, (int)Main.GameUpdateCount, .8f);
    }

    internal static void Wake(Vector2 p, Vector2 velocity, SpreadArt art, int phase)
    {
        if (!SpreadVfxBudget.Visible(p, 180f)) return;
        Vector2 aim = velocity.SafeNormalize(Vector2.UnitX), side = new(-aim.Y, aim.X);
        int streams = Large(art) ? 2 : 1;
        for (int i = 0; i < streams; i++)
        {
            float offset = MathF.Sin(phase * .42f) * (Large(art) ? 9f : 3f) * (i == 0 ? 1f : -1f);
            Vector2 root = p + side * offset - aim * Main.rand.NextFloat(2f, 14f);
            // Short-lived solid soft light; no hollow, branching flame silhouettes.
            SpreadVfxDetail.Fire(root, -aim * .65f + side * offset * .08f,
                Large(art) ? 19f : 12f, 13, 28f);
        }
        for (int i = 0; i < (Large(art) ? 4 : 2); i++)
            DustFire(p - aim * Main.rand.NextFloat(0f, Math.Min(60f, velocity.Length() * 3f)),
                -aim * Main.rand.NextFloat(.4f, 2f) + Main.rand.NextVector2Circular(.8f, .8f), phase + i, .9f);
    }

    internal static void Release(Vector2 p, Vector2 direction, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(p)) return;
        Vector2 aim = direction.SafeNormalize(Vector2.UnitX);
        SpreadChargeVfx.Pulse(p, aim, art, true);
        for (int i = 0; i < 8; i++)
            DustFire(p, aim.RotatedBy(Main.rand.NextFloat(-.35f, .35f)) * Main.rand.NextFloat(3f, 7f), i, 1f);
    }

    internal static void Burst(Vector2 p, float diameter, float progress, SpreadArt art)
    {
        diameter = MathHelper.Clamp(diameter, 16f, Large(art) ? 500f : 300f);
        if (!SpreadVfxBudget.Visible(p, diameter * .5f)) return;
        float t = MathHelper.Clamp(progress, 0f, 1f);
        float fade = MathF.Pow(1f - t, 2f);
        float expansion = 1f - MathF.Pow(1f - t, 3f);
        using (SpreadArtsMaterial.Begin())
        {
            Glow(p, new Vector2(diameter * (.3f + .65f * expansion)), SpreadArtsVfx.Body(art), fade * .72f);
            Glow(p, new Vector2(diameter * (.1f + .22f * expansion)), SpreadArtsVfx.Edge(art), fade);
        }
    }

    internal static void Debris(Vector2 p, SpreadArt art, float radius)
    {
        if (!SpreadVfxBudget.Visible(p, radius)) return;
        radius = MathHelper.Clamp(radius, 30f, Large(art) ? 250f : 150f);
        int count = Large(art) ? 28 : 18;
        for (int i = 0; i < count; i++)
        {
            Vector2 ray = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.08f, .08f)).ToRotationVector2();
            float speed = Main.rand.NextFloat(5f, 12f) * radius / 150f;
            DustFire(p + ray * 8f, ray * speed, i, 1.2f);
            if (i % 3 == 0)
                SpreadVfxDetail.Fire(p, ray * speed * .65f, Large(art) ? 26f : 19f, 20, radius * .75f);
            if (i % 2 == 0 && SpreadVfxBudget.Take(p))
                OperatorFlightVfx.Spark(p, ray * speed, SpreadArtsVfx.Edge(art), 14f, 15, 1.2f);
        }
    }

    internal static void FieldTick(Vector2 p, float radius, int age, int remaining, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(p, radius) || remaining <= 6) return;
        float fade = MathHelper.Clamp(remaining / 18f, 0f, 1f);
        for (int i = 0; i < (Large(art) ? 18 : 6); i++)
        {
            Vector2 root = p + Main.rand.NextVector2Circular(radius * .7f, radius * .2f);
            Vector2 drift = new(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-4.5f, -1.5f));
            if (age < 8) { root = p + Main.rand.NextVector2Circular(14f, 14f); drift = Main.rand.NextVector2Unit() * radius * .065f; }
            DustFire(root, drift, age + i, Main.rand.NextFloat(.7f, 1.2f) * fade);
        }
        if (age % 2 == 0 && remaining > 14)
            for (int i = 0; i < (Large(art) ? 4 : 2); i++)
            {
                Vector2 root = p + Main.rand.NextVector2Circular(radius * .62f, 20f);
                SpreadVfxDetail.Fire(root, new Vector2(Main.rand.NextFloat(-.8f, .8f), Main.rand.NextFloat(-3f, -1.5f)),
                    Main.rand.NextFloat(18f, Large(art) ? 36f : 27f), Math.Min(22, remaining), 65f);
            }
        Lighting.AddLight(p, SpreadArtsVfx.Body(art).ToVector3() * .35f * fade);
    }

    internal static void FieldDraw(Vector2 p, float radius, int age, int remaining, SpreadArt art)
    {
        if (!SpreadVfxBudget.Visible(p, radius)) return;
        float fade = MathHelper.Clamp(age / 4f, 0f, 1f) * MathHelper.Clamp(remaining / 20f, 0f, 1f);
        using (SpreadArtsMaterial.Begin())
        {
            Glow(p, new Vector2(radius * 1.5f, 48f), SpreadArtsVfx.Body(art),
                (Large(art) ? .8f : .4f) * fade);
            Glow(p - Vector2.UnitY * 5f, new Vector2(radius * .8f, 22f), SpreadArtsVfx.Edge(art),
                (Large(art) ? .36f : .18f) * fade);
        }
    }
}
