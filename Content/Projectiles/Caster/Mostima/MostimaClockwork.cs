using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// StarsofDestinyLEFTCLK reference: independent double tracks, twelve fixed indices,
// and two unequal hands assembled from living points. Geometry is reimplemented locally;
// no Calamity particles, invisible damage projectiles, assets or runtime references.
internal static class MostimaClockwork
{
    internal const float Radius = 280f;
    internal const int Lifetime = 325;
    internal const int MoveTicks = 12;
    internal const int BeatTicks = 17;
    internal const int FadeTicks = 15;
    internal const float BlastRadius = 620f;
    internal static bool IsBeat(int age) => age >= MoveTicks && age % BeatTicks == MoveTicks;
    internal static float BeatAge(float age) => age < MoveTicks ? -1f : (age - MoveTicks) % BeatTicks;
    internal static readonly Color Silver = new(198, 231, 255);
    internal static readonly Color Brass = new(227, 189, 118);
    private static readonly string[] RomanHours = { "XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI" };
    internal static Color TimeColor(Color normal, float gold) => Color.Lerp(normal, new Color(255, 191, 74), MathHelper.Clamp(gold, 0f, 1f));

    internal static float FaceOpacity(float age)
        => MathHelper.Clamp(age / 12f, 0f, 1f) * .5f;

    // One minute-hand revolution / one hour division over the entire released clock.
    internal static float HandAngle(float age, bool minute)
    {
        float step = MathF.Floor(age / BeatTicks);
        float t = MathHelper.Clamp(age % BeatTicks / MoveTicks, 0f, 1f);
        float progress = step + 1f - MathF.Pow(1f - t, 3f);
        return -MathHelper.PiOver2 + (minute ? progress * MathHelper.Pi / 6f : .72f + progress * MathHelper.Pi / 72f);
    }

    internal static Vector2 Point(int track, int index, int count, float age)
    {
        float angle = index * MathHelper.TwoPi / count - MathHelper.PiOver2;
        float radius;
        if (track >= 3)
        {
            angle = HandAngle(age, track == 4);
            radius = MathHelper.Lerp(-16f, track == 4 ? 211f : 132f, index / (float)(count - 1));
        }
        else radius = track == 0 ? 270f : track == 1 ? 225f : (index % 5 == 0 ? 245f : 258f);
        float gather = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / 9f, 0f, 1f));
        // Start outside each destination, but never outside the 280px collision circle.
        radius = MathHelper.Lerp(radius, Math.Min(276f, radius + 28f), gather);
        angle += (track < 2 ? (track == 0 ? 1 : -1) * .18f : 0f) * gather;
        float breakaway = 0f;
        angle += (track % 2 == 0 ? 1f : -1f) * breakaway * breakaway * .045f;
        return angle.ToRotationVector2() * radius;
    }

    internal static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (!float.IsFinite(length) || length < .01f || length > 300f) return;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0, .5f),
            new Vector2(length, MathHelper.Clamp(width, .5f, 6f)), SpriteEffects.None, 0f);
    }

    private static void Arc(Vector2 center, float radius, float start, float sweep, Color tint, float width)
    {
        int count = Math.Clamp((int)(MathF.Abs(sweep) * radius / 9f), 2, 192);
        Vector2 previous = center + start.ToRotationVector2() * radius;
        for (int i = 1; i <= count; i++)
        {
            Vector2 next = center + (start + sweep * i / count).ToRotationVector2() * radius;
            Line(previous, next, tint, width);
            previous = next;
        }
    }

    // Called inside an additive pass. Sparse structural lines retain readability if the
    // particle budget is exhausted; the luminous beadwork is supplied by the local manager.
    internal static void Face(Vector2 center, float radius, float age, float opacity, bool preview = false, float gold = 0f)
    {
        float scale = radius / Radius;
        float reveal = preview ? 1f : MathHelper.Clamp((age + 2f) / 8f, 0, 1);
        Arc(center, radius * .964f, -MathHelper.PiOver2, MathHelper.TwoPi * reveal, TimeColor(MostimaVisuals.Blue, gold) * (.65f * opacity), 1.6f);
        Arc(center, radius * .804f, -MathHelper.PiOver2, -MathHelper.TwoPi * reveal, TimeColor(Brass, gold) * (.38f * opacity), 1f);
        for (int i = 0; i < 60; i++)
        {
            if (i / 60f > reveal) break;
            Vector2 ray = (i * MathHelper.TwoPi / 60f - MathHelper.PiOver2).ToRotationVector2();
            bool hour = i % 5 == 0;
            Line(center + ray * radius * (hour ? .84f : .915f), center + ray * radius * .947f,
                TimeColor(hour ? Brass : MostimaVisuals.Blue, gold) * opacity * (hour ? .92f : .55f), hour ? 3f * scale : scale);
        }
        // Twelve upright numerals belong to the stationary clock, not the moving flow.
        for (int hour = 0; hour < RomanHours.Length; hour++)
        {
            Vector2 position = center + (hour * MathHelper.Pi / 6f - MathHelper.PiOver2).ToRotationVector2() * 195f * scale;
            Numeral(position, RomanHours[hour], scale * (hour % 3 == 0 ? 1f : .8f), TimeColor(Brass, gold) * opacity);
        }
        for (int h = 0; h < 2; h++)
        {
            float angle = preview ? -MathHelper.PiOver2 + (h == 1 ? age * .025f : .72f + age * .025f / 12f) : HandAngle(age, h == 1);
            Vector2 ray = angle.ToRotationVector2(), side = ray.RotatedBy(MathHelper.PiOver2);
            float length = (h == 1 ? 211f : 132f) * scale;
            Vector2 tip = center + ray * length;
            Color tint = TimeColor(h == 1 ? Silver : Brass, gold) * opacity;
            Line(center - ray * 16f * scale, tip, tint * .8f, (h == 1 ? 2f : 3.5f) * scale);
            Vector2 shoulder = center + ray * length * .77f;
            Line(shoulder + side * 7f * scale, tip, tint, 1.5f * scale);
            Line(shoulder - side * 7f * scale, tip, tint, 1.5f * scale);
            Line(shoulder + side * 7f * scale, center + ray * length * .54f, tint * .7f, scale);
            Line(shoulder - side * 7f * scale, center + ray * length * .54f, tint * .7f, scale);
        }
        MostimaVisuals.Sprite("TemporalOrb", center, new Vector2(18f * scale), TimeColor(Silver, gold) * opacity);
    }

    internal static void Backplate(Vector2 center, float opacity, float scale = 1f, float gold = 0f)
    {
        // World-sized filled indigo face. Explicit one-pixel source rectangle.
        float radius = Radius * .97f * scale;
        for (int y = -(int)radius; y <= (int)radius; y += 2)
        {
            float half = MathF.Sqrt(Math.Max(0f, radius * radius - y * y));
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, center + new Vector2(-half, y) - Main.screenPosition,
                new Rectangle(0, 0, 1, 1), Color.Lerp(new Color(12, 23, 58), new Color(59, 31, 9), gold) * opacity, 0f, Vector2.Zero,
                new Vector2(half * 2f, 2f), SpriteEffects.None, 0f);
        }
    }

    private static void Numeral(Vector2 center, string text, float scale, Color tint)
    {
        for (int i = 0; i < text.Length; i++)
        {
            Vector2 p = center + Vector2.UnitX * (i - (text.Length - 1) * .5f) * 10f * scale;
            Vector2 a = p + new Vector2(-3, -7) * scale, b = p + new Vector2(3, -7) * scale;
            Vector2 c = p + new Vector2(-3, 7) * scale, d = p + new Vector2(3, 7) * scale;
            if (text[i] == 'I') { Line(a, b, tint, scale); Line(c, d, tint, scale); Line((a + b) / 2, (c + d) / 2, tint, 1.5f * scale); }
            else if (text[i] == 'X') { Line(a, d, tint, 1.5f * scale); Line(b, c, tint, 1.5f * scale); }
            else { Line(a, (c + d) / 2, tint, 1.5f * scale); Line(b, (c + d) / 2, tint, 1.5f * scale); }
        }
    }

    internal static void Assemble(Vector2 center)
    {
        if (!SpreadVfxBudget.Visible(center, Radius + 40f)) return;
        int[] counts = { 96, 72, 60, 19, 29 };
        for (int track = 0; track < counts.Length; track++)
        for (int i = 0; i < counts[track]; i++)
        {
            if (ParticleManager.activeParticles.Count >= 1300) return;
            new T_Dial_Point_Particle(center, track, i, counts[track]).Spawn();
        }
    }
}
