using System;
using Microsoft.Xna.Framework;
using Terraria;
using MoteMaterial = ArknightsMod.Common.Particle.T.T_Crossbow_Mote_Particle.Material;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// Composition only: no damage, debuffs, movement changes, or external mod assets.
internal static class CrossbowParticleCompositions
{
    private readonly record struct Palette(Color Main, Color Second, Color Core);
    private static Palette Colors(CrossbowKind kind) => kind switch {
        CrossbowKind.Kroos => new(new(244, 167, 68), new(209, 114, 49), new(255, 231, 170)),
        CrossbowKind.KroosAlter => new(new(71, 223, 181), new(104, 174, 238), new(214, 255, 247)),
        CrossbowKind.Melanite => new(new(164, 81, 244), new(98, 64, 224), new(240, 197, 255)),
        CrossbowKind.Provence => new(new(186, 112, 228), new(255, 173, 75), new(255, 227, 173)),
        CrossbowKind.Schwarz => new(new(155, 82, 213), new(78, 37, 120), new(233, 188, 255)),
        CrossbowKind.GreyThroat => new(new(147, 208, 241), new(91, 145, 211), new(239, 253, 255)),
        CrossbowKind.BluePoison => new(new(68, 227, 153), new(100, 142, 242), new(211, 255, 226)),
        _ => new(new(255, 133, 192), new(176, 81, 169), new(255, 219, 238)) };

    internal static void Flight(Projectile p, CrossbowKind kind, Vector2 previous, int frame)
    {
        Palette colors = Colors(kind);
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX), side = aim.RotatedBy(MathHelper.PiOver2);
        bool heavy = CrossbowSignatureVfx.Heavy(kind);
        // Independent random distributions keep repeated shots from printing identical ornaments.
        // One broad layer, one bright grain, and intermittent tiny dust have different lifetimes.
        int cadence = kind == CrossbowKind.Kroos ? 3 : heavy ? 1 : 2;
        if ((frame + p.identity) % cadence == 0)
        {
            float t = Main.rand.NextFloat();
            Vector2 at = Vector2.Lerp(previous, p.Center, t) - aim * Main.rand.NextFloat(3f, 12f);
            float width = kind switch {
                CrossbowKind.Melanite => 12f, CrossbowKind.Provence => 8f, CrossbowKind.Schwarz => 9f,
                CrossbowKind.BluePoison => 8f, CrossbowKind.Pozemka => 7f, _ => 5f };
            float phase = frame * .44f + p.identity * 1.71f;
            float offset = MathF.Sin(phase) * width;
            at += side * (offset + Main.rand.NextFloat(-3f, 3f));
            Vector2 velocity = -aim * Main.rand.NextFloat(1.4f, heavy ? 4.2f : 3f)
                + side * Main.rand.NextFloat(-1.4f, 1.4f);
            Color color = Color.Lerp(colors.Main, colors.Second, Main.rand.NextFloat());
            float curl = kind switch {
                CrossbowKind.Melanite => offset > 0 ? .09f : -.09f,
                CrossbowKind.BluePoison => Main.rand.NextFloat(-.08f, .08f),
                CrossbowKind.Pozemka => .065f, _ => Main.rand.NextFloat(-.035f, .035f) };
            CrossbowParticles.Energy(at, velocity, color, colors.Second,
                Main.rand.NextFloat(heavy ? 7f : 5f, heavy ? 11f : 8f),
                Main.rand.Next(heavy ? 14 : 10, heavy ? 23 : 17), curl,
                glint: (frame + p.identity) % 4 == 0);
            if (kind is CrossbowKind.BluePoison or CrossbowKind.Pozemka)
                CrossbowParticles.Mote(at, velocity.RotatedByRandom(.5f),
                    kind == CrossbowKind.Pozemka ? new(130, 38, 82) : colors.Second,
                    5f, 18, kind == CrossbowKind.Pozemka ? MoteMaterial.Ink : MoteMaterial.Venom);
        }
        if ((frame + p.identity) % (heavy ? 2 : 4) == 0)
        {
            Vector2 at = Vector2.Lerp(previous, p.Center, Main.rand.NextFloat())
                + Main.rand.NextVector2Circular(10f, 10f);
            CrossbowParticles.Gem(at, -aim * Main.rand.NextFloat(.3f, 1.5f),
                Main.rand.NextBool() ? colors.Main : colors.Core, Main.rand.NextFloat(.45f, .8f));
        }
        if (kind == CrossbowKind.Melanite && frame % 4 == 0)
            CrossbowParticles.Energy(p.Center, -aim * 3f, colors.Core, colors.Main, 8f, 12, -.04f, true);
        if (kind == CrossbowKind.Schwarz && frame % 3 == 0)
            CrossbowParticles.Wisp(p.Center - aim * 12f + Main.rand.NextVector2Circular(4f, 4f),
                -aim * .6f, new(64, 28, 89), 27f, 22, .4f);
    }

    internal static void Impact(Projectile p, CrossbowKind kind, float strength)
    {
        Palette colors = Colors(kind);
        Vector2 center = p.Center, aim = p.velocity.SafeNormalize(Vector2.UnitX);
        float angle = aim.ToRotation();
        bool heavy = CrossbowSignatureVfx.Heavy(kind);
        float diameter = kind switch {
            CrossbowKind.Kroos => 34f, CrossbowKind.KroosAlter => 52f,
            CrossbowKind.Melanite => 108f, CrossbowKind.Provence => 122f,
            CrossbowKind.Schwarz => 132f, CrossbowKind.GreyThroat => 48f,
            CrossbowKind.BluePoison => 66f, _ => 72f };
        diameter *= strength;
        int life = heavy ? 19 : kind == CrossbowKind.Kroos ? 10 : 13;
        // Front flashes immediately; the broken secondary wave catches up after the core fades.
        Vector2 front = kind switch {
            CrossbowKind.Provence => new(.68f, 1.1f),
            CrossbowKind.Schwarz => new(.48f, 1.15f),
            CrossbowKind.GreyThroat => new(.6f, 1f),
            CrossbowKind.BluePoison => new(1f, .8f), _ => Vector2.One };
        CrossbowParticles.Shock(center, colors.Core, front * diameter, angle, life,
            opacity: heavy ? .88f : .72f);
        if (kind != CrossbowKind.Kroos)
            CrossbowParticles.Shock(center, kind is CrossbowKind.BluePoison or CrossbowKind.Pozemka
                ? colors.Second : colors.Main, new Vector2(diameter * 1.12f, diameter * .84f),
                angle + Main.rand.NextFloat(-.3f, .3f), life + 4, 3, true, heavy ? .72f : .55f);
        CrossbowParticles.Burst(center, colors.Main, colors.Core,
            new Vector2(diameter * (heavy ? .85f : .72f), diameter * (heavy ? .72f : .6f)),
            angle + Main.rand.NextFloat(-.4f, .4f), heavy ? 12 : 8);

        int grains = (int)((heavy ? 15 : kind == CrossbowKind.Kroos ? 5 : 9) * strength);
        for (int i = 0; i < grains; i++)
        {
            // Combine a directional penetration spray and an irregular radial splash.
            Vector2 direction = i % 3 == 0 ? Main.rand.NextVector2Unit()
                : aim.RotatedByRandom(kind == CrossbowKind.BluePoison ? 1.5f : 1.05f);
            Vector2 velocity = direction * Main.rand.NextFloat(heavy ? 2.5f : 1.5f, heavy ? 8.5f : 5.5f) * strength;
            Color color = Color.Lerp(colors.Main, colors.Second, Main.rand.NextFloat());
            float curl = Main.rand.NextFloat(-.07f, .07f);
            if (kind == CrossbowKind.Melanite)
            {
                // The two helical streams unwind on impact instead of becoming a generic spray.
                float sign = i % 2 == 0 ? 1f : -1f;
                velocity = aim.RotatedBy(sign * (.25f + i * .11f))
                    * Main.rand.NextFloat(3f, 8f) * strength;
                curl = sign * .11f;
            }
            CrossbowParticles.Energy(center + Main.rand.NextVector2Circular(5f, 5f), velocity,
                i % 4 == 0 ? colors.Core : color, colors.Second,
                Main.rand.NextFloat(heavy ? 7f : 5f, heavy ? 13f : 9f) * strength,
                Main.rand.Next(heavy ? 17 : 12, heavy ? 28 : 21),
                curl, i % 4 == 0, true);
            if (i % 2 == 0)
                CrossbowParticles.Gem(center, velocity * Main.rand.NextFloat(.45f, .9f), color,
                    Main.rand.NextFloat(.65f, 1.15f) * strength, true);
        }
        int sparks = heavy ? 10 : kind == CrossbowKind.Kroos ? 3 : 5;
        for (int i = 0; i < sparks; i++)
        {
            Vector2 velocity = aim.RotatedByRandom(1.2f) * Main.rand.NextFloat(2f, heavy ? 10f : 6f) * strength;
            CrossbowParticles.Spark(center, velocity, i % 2 == 0 ? colors.Core : colors.Second,
                Main.rand.NextFloat(heavy ? 22f : 12f, heavy ? 40f : 22f) * strength,
                heavy ? 4.5f : 3f, Main.rand.Next(10, heavy ? 23 : 17),
                kind == CrossbowKind.Provence ? .09f : .025f, true);
        }
        int clouds = heavy ? 4 : kind is CrossbowKind.BluePoison or CrossbowKind.Pozemka ? 3 : 1;
        for (int i = 0; i < clouds; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f) + aim * .6f;
            Color color = kind == CrossbowKind.Schwarz ? new(52, 24, 71)
                : kind == CrossbowKind.BluePoison ? Color.Lerp(colors.Main, colors.Second, i / (float)clouds)
                : Color.Lerp(colors.Main, colors.Second, .4f);
            CrossbowParticles.Wisp(center + Main.rand.NextVector2Circular(7f, 7f), velocity,
                color, Main.rand.NextFloat(diameter * .35f, diameter * .55f),
                heavy ? 27 : 21, heavy ? .48f : .38f, true);
        }
        if (kind is CrossbowKind.BluePoison or CrossbowKind.Pozemka)
        {
            for (int i = 0; i < 6; i++)
                CrossbowParticles.Mote(center, Main.rand.NextVector2Circular(4f, 4f) + aim,
                    kind == CrossbowKind.Pozemka ? new(129, 37, 81) : colors.Second,
                    7f * strength, 23, kind == CrossbowKind.Pozemka ? MoteMaterial.Ink : MoteMaterial.Venom, true);
        }
        if (kind == CrossbowKind.Schwarz)
        {
            // Dark tears give the pale explosion a readable centre on bright backgrounds.
            Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++)
                CrossbowParticles.Loose(CrossbowMaterial.Smear, center + side * (i - 1) * 5f,
                    aim * (1f + i) + side * (i - 1) * .8f,
                    new(39, 16, 57), new Vector2(64f, 14f) * strength, 17,
                    angle + Main.rand.NextFloat(-.25f, .25f), spin: .025f * (i - 1),
                    growth: -.6f, bright: false, opacity: .8f, impact: true);
        }
        Lighting.AddLight(center, colors.Main.ToVector3() * (heavy ? .65f : .35f) * strength);
    }
}
