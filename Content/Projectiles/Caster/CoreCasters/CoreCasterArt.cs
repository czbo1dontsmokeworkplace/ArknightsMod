using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// Normal premultiplied color layers own the body; explicitly named Glow is the only additive path.
internal static class CoreCasterArt
{
    internal const string Assets = "ArknightsMod/Content/Textures/CoreCasters/";
    internal const int CrimsonPalette = 10;
    internal static readonly Color Crimson = new(183, 30, 80);
    private static Texture2D ball, spark, bloom, arc, pressure, fire, ash, crystal, soul, veil, wind, needle, point, ring, circle;
    internal static bool Ready => ball != null && !ball.IsDisposed;
    internal static void Assign(Func<string, Texture2D> read)
    {
        ball = read("PurifiedGelBall"); CoreCasterPalette.Assign(ball);
        spark = read("Spark"); bloom = read("GelBloomCircle"); ring = read("GelBloomRing");
        point = read("GelPoint"); circle = read("GelCircle"); arc = read("DreamArc"); pressure = read("Pressure");
        fire = read("FireTufts"); ash = read("AshCloud"); crystal = read("Crystal"); soul = read("Soul");
        veil = read("Veil"); wind = read("WindCrescent"); needle = read("TechNeedle");
    }
    internal static void Unload()
    {
        CoreCasterPalette.Unload(); ball = spark = bloom = arc = pressure = fire = ash = crystal = soul = veil = wind = needle = point = ring = circle = null;
    }
    internal static Color ColorFor(CoreCasterKind kind) => CoreCasterPalette.For((int)kind).Body;
    internal static Color Accent(CoreCasterKind kind, Color color) => CoreCasterPalette.For(Key(kind, color)).Ring;
    private static int Key(CoreCasterKind kind, Color? color = null) => CoreCasterPalette.Key(kind, color.HasValue && color.Value == Crimson);
    private static Color Tint(Color c, float opacity, bool unused = false) => c * Math.Clamp(opacity, 0, 1);
    private static void Sprite(SpriteBatch batch, Texture2D tex, Vector2 at, Vector2 size, float rotation, Color tint, Rectangle? source = null)
    {
        Rectangle rect = source ?? new(0, 0, tex.Width, tex.Height);
        batch.Draw(tex, at, rect, tint, rotation, new Vector2(rect.Width, rect.Height) / 2,
            size / new Vector2(rect.Width, rect.Height), SpriteEffects.None, 0);
    }
    private static void Ink(SpriteBatch batch, Texture2D tex, Vector2 at, Vector2 size, float angle, int key, float opacity, Rectangle? source = null) =>
        Sprite(batch, CoreCasterPalette.Material(tex, key), at, size, angle, Color.White * Math.Clamp(opacity, 0, 1), source);
    private static void Glow(SpriteBatch batch, Vector2 at, Vector2 size, Color color, float opacity, float rotation = 0) =>
        Sprite(batch, bloom, at, size, rotation, new Color(color.R, color.G, color.B, 0) * Math.Clamp(opacity, 0, 1));
    private static Rectangle Frame(Texture2D tex, int height, int frame) => new(0, Math.Abs(frame % (tex.Height / height)) * height, tex.Width, height);
    private static float Angle(Vector2 v) => MathF.Atan2(v.Y, v.X);
    private static Vector2 Unit(Vector2 v) => v.LengthSquared() > .001f ? Vector2.Normalize(v) : Vector2.UnitX;
    private static Vector2 Circle(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    internal static void Head(SpriteBatch batch, Vector2 at, Vector2 direction, CoreCasterKind kind, float size, float time, int seed,
        float opacity = 1, bool additive = false, Color? overrideColor = null)
    {
        int key = Key(kind, overrideColor); var palette = CoreCasterPalette.For(key);
        Vector2 forward = Unit(direction), side = new(-forward.Y, forward.X);
        float angle = Angle(direction), spin = time * 26.4f + seed * .73f;
        Rectangle frame = Frame(ball, ball.Height / 8, (int)(time * 40) + seed);
        Vector2 bodySize = new(size * 2, size * 2 * 36 / 34);
        Texture2D atlas = CoreCasterPalette.Ball(key);
        // Like PurifiedGel_Ball: one larger faint halo, then ONE normally blended animated body.
        // The opaque source body must always remain visible above the halo and shells.
        Sprite(batch, atlas, at, bodySize * 1.5f, spin, new Color(255, 255, 255, 0) * (opacity * .12f), frame);
        Glow(batch, at, new Vector2(size * 3.5f), palette.Body, opacity * .11f);
        switch (kind) {
            case CoreCasterKind.Durin:
                Ink(batch, veil, at - forward * size * .65f, new Vector2(size * 2.7f, size * .9f), angle, key, opacity * .45f);
                break;
            case CoreCasterKind.Steward:
                for (int i = 0; i < 3; i++) {
                    float orbit = -time * 3.5f + seed + i * MathHelper.TwoPi / 3;
                    Vector2 offset = Circle(orbit) * new Vector2(size * 1.45f, size * .95f);
                    Ink(batch, crystal, at + offset, new Vector2(size * 1.05f, size * .45f), orbit + .4f, key, opacity * .92f);
                }
                break;
            case CoreCasterKind.Haze:
                for (int i = 0; i < 3; i++) {
                    float phase = time * 5 + seed + i * 2.1f;
                    Vector2 offset = -forward * size * (.35f + i * .38f) + side * MathF.Sin(phase) * size * .4f;
                    Ink(batch, ash, at + offset, new Vector2(size * 2.5f, size * 1.8f), phase * .12f, key, opacity * (.38f - i * .06f), Frame(ash, 192, seed + i));
                    Ink(batch, veil, at + offset, new Vector2(size * 3.8f, size * 2.1f), angle + MathF.Sin(phase) * .18f, key, opacity * (.7f - i * .16f));
                }
                break;
            case CoreCasterKind.Nightmare:
                for (int i = 0; i < 2; i++) {
                    float orbit = time * 3.1f + seed + i * 2.7f;
                    Ink(batch, arc, at + Circle(orbit) * size * .38f, new Vector2(size * (3.4f + i * .6f), size * 2.3f), orbit, key, opacity * (.95f - i * .3f));
                    Ink(batch, soul, at - forward * size * .8f + side * MathF.Sin(orbit) * size * .9f,
                        new Vector2(size * .85f, size * .45f), angle + orbit * .1f, key, opacity * .65f);
                }
                break;
            case CoreCasterKind.Absinthe:
                Ink(batch, needle, at - forward * size * .9f, new Vector2(size * 4.5f, size * .55f), angle, key, opacity * .92f);
                for (int i = -1; i <= 1; i += 2)
                    Ink(batch, crystal, at - forward * size * .7f + side * size * i * 1.1f,
                        new Vector2(size * 1.1f, size * .25f), angle, key, opacity * .85f);
                break;
            case CoreCasterKind.Eyjafjalla:
                Flame(batch, at - forward * size * .7f, forward, size * 3.1f, .15f, seed + (int)(time * 18), opacity * .95f);
                break;
            case CoreCasterKind.Hoolheyak:
                for (int i = 0; i < 3; i++) {
                    float phase = angle + time * 2.6f + i * 2.4f;
                    Ink(batch, wind, at - forward * size * (.25f + i * .4f), new Vector2(size * (3.8f + i * .35f), size * 2.5f), phase, key, opacity * (.9f - i * .2f));
                }
                break;
        }
        Sprite(batch, atlas, at, bodySize, spin, Color.White * opacity, frame);
    }
    internal static void Bolt(SpriteBatch batch, Vector2 center, Vector2 velocity, Vector2[] history, Vector2 offset,
        CoreCasterKind kind, int mode, float time, int seed)
    {
        int key = CoreCasterPalette.Key(kind, kind == CoreCasterKind.Haze && mode == 2);
        float limit = CoreCasterTrail.LengthFor(kind, mode), distance = 0, nextEcho = 25;
        Vector2 previous = center;
        foreach (Vector2 old in history) {
            if (old == Vector2.Zero) break;
            Vector2 at = old + offset, delta = previous - at; float step = delta.Length();
            if (!float.IsFinite(step) || step > 96) break;
            distance += step; if (distance > limit) break;
            if (distance >= nextEcho) {
                nextEcho += kind == CoreCasterKind.Absinthe ? 35 : 29;
                float fade = MathF.Pow(1 - distance / limit, 1.6f);
                Texture2D tex = kind switch { CoreCasterKind.Steward => crystal, CoreCasterKind.Haze => ash,
                    CoreCasterKind.Nightmare => point, CoreCasterKind.Hoolheyak => wind, _ => spark };
                if (kind == CoreCasterKind.Eyjafjalla) Flame(batch, at, delta, 26 * fade, .35f + (1 - fade) * .55f, seed, .65f * fade);
                else if (kind == CoreCasterKind.Haze)
                    Ink(batch, ash, at, new Vector2(45, 28) * (.7f + (1 - fade) * .4f),
                        seed + distance * .003f, key, fade * .3f, Frame(ash, 192, seed + (int)(distance / 29)));
                else Ink(batch, tex, at, (kind == CoreCasterKind.Nightmare ? new Vector2(6, 16) : new Vector2(28, 12)) * (.5f + fade * .5f),
                    Angle(delta) + (kind == CoreCasterKind.Nightmare ? MathHelper.PiOver2 : MathF.Sin(time * 5 + distance * .02f) * .18f), key, fade * .65f);
            }
            previous = at;
        }
        float size = kind switch { CoreCasterKind.Durin => 11, CoreCasterKind.Steward => 15, CoreCasterKind.Absinthe => 13,
            CoreCasterKind.Eyjafjalla => mode == 3 ? 15 : 18, _ => 18 };
        Head(batch, center, velocity, kind, size * (mode > 0 ? 1.08f : 1), time, seed,
            overrideColor: key == 7 ? Crimson : null);
    }
    internal static void Flame(SpriteBatch batch, Vector2 at, Vector2 direction, float size, float maturity, int seed, float opacity = 1, bool additive = false)
    {
        float fade = MathF.Pow(1 - Math.Clamp(maturity, 0, 1), .75f) * opacity;
        float angle = Angle(direction) + MathF.Sin(seed * 1.7f + maturity * 8) * .1f;
        Rectangle frame = Frame(fire, 128, seed + (int)(maturity * 9));
        Ink(batch, ash, at - direction * .1f, new Vector2(size * 1.4f, size), angle, 5, fade * .22f, Frame(ash, 192, seed));
        Ink(batch, fire, at, new Vector2(size * 1.3f, size * .95f), angle, 5, fade, frame);
        Glow(batch, at, new Vector2(size * 1.6f, size), CoreCasterPalette.For(5).Ring, fade * .1f, angle);
    }
    internal static void Mote(SpriteBatch batch, Vector2 at, Vector2 velocity, CoreCasterKind kind, float size, float progress, int seed, int variant)
    {
        int key = CoreCasterPalette.Key(kind, variant >= CrimsonPalette); var p = CoreCasterPalette.For(key);
        variant %= CrimsonPalette; progress = Math.Clamp(progress, 0, 1);
        float fade = 1 - progress * progress, angle = Angle(velocity);
        if (variant == 1)
            Ink(batch, ash, at, new Vector2(size * (.8f + progress * .7f)), seed + progress * .2f, key,
                MathF.Sin(progress * MathHelper.Pi) * .32f, Frame(ash, 192, seed));
        else if (variant == 2) Burst(batch, at, kind, size, progress, seed, overrideColor: key == 7 ? Crimson : null);
        else if (variant == 3) Flame(batch, at, velocity, size * (1 + progress * .5f), progress, seed, .7f);
        else if (variant == 4) {
            Ink(batch, veil, at, new Vector2(size * 2.6f, size * 1.5f) * (1 + progress), angle + progress + seed, key, fade * .6f);
            Glow(batch, at, new Vector2(size * 1.6f), p.Body, fade * .08f);
        }
        else if (variant == 5) {
            // Port PointParticle's double narrow draw and cubic fade; keep the colored center.
            float shrink = MathF.Pow(.95f, progress * 14), alpha = 1 - progress * progress * progress;
            Vector2 shape = new Vector2(size * .44f, size * 1.25f) * shrink;
            Ink(batch, point, at, shape, angle + MathHelper.PiOver2, key, alpha * .85f);
            Ink(batch, point, at, shape * new Vector2(.45f, 1), angle + MathHelper.PiOver2, key, alpha * .65f);
            Glow(batch, at, new Vector2(size * 1.9f), p.Body, alpha * .15f);
        }
        else if (variant == 6) {
            // SquashDust: soft halo around a smaller solid colored bead, squeezed by velocity.
            float squeeze = Math.Clamp(velocity.Length() / 6, 0, 1);
            Vector2 shape = new Vector2(1 - squeeze * .4f, 1 + squeeze * 1.1f) * size * (1 - progress);
            Glow(batch, at, shape * 3.2f, p.Body, fade * .28f, angle + MathHelper.PiOver2);
            Ink(batch, circle, at, shape * .65f, angle + MathHelper.PiOver2, key, fade * .72f);
        }
        else {
            Ink(batch, crystal, at, new Vector2(size * .45f, size * .3f) * (1 - progress * .55f), angle + seed, key, fade);
            Glow(batch, at, new Vector2(size), p.Body, fade * .13f);
        }
    }
    internal static void Burst(SpriteBatch batch, Vector2 at, CoreCasterKind kind, float radius, float progress, int seed, bool additive = false, Color? overrideColor = null)
    {
        int key = Key(kind, overrideColor); var p = CoreCasterPalette.For(key);
        progress = Math.Clamp(progress, 0, 1); float fade = MathF.Pow(1 - progress, 1.35f), grow = 1 - MathF.Pow(1 - progress, 3);
        if (kind == CoreCasterKind.Eyjafjalla) for (int i = 0; i < (radius > 80 ? 6 : 4); i++) {
            Vector2 drift = Circle(seed * .3f + i * 2.399963f) * new Vector2(.65f, .4f) * radius * grow * .65f;
            Flame(batch, at + drift, new Vector2(drift.X * .015f, -1), radius * (.52f + i % 3 * .08f), progress * .85f, seed + i, fade);
        }
        Ink(batch, ring, at, new Vector2(radius * (1 + grow * 1.8f)), seed * .31f, key, fade * .75f);
        Ink(batch, pressure, at, new Vector2(radius * 2.7f * grow, radius * 2.1f * grow), seed * .31f, key, fade * .6f, Frame(pressure, 256, seed + (int)(progress * 5)));
        Glow(batch, at, new Vector2(radius * (1 + grow)), p.Body, fade * .24f);
        Glow(batch, at, new Vector2(radius * .6f), p.Ring, Math.Max(0, 1 - progress * 6) * .5f);
    }
    internal static void WindField(SpriteBatch batch, Vector2 center, float age, float scale, float opacity, int seed)
    {
        for (int i = 0; i < 7; i++) {
            float t = (i / 7f + age * .012f) % 1, fade = MathF.Sin(t * MathHelper.Pi) * opacity;
            float width = (26 + t * 68) * scale;
            Vector2 at = center + new Vector2(MathF.Sin(seed + t * 7 + age * .05f) * 6, 45 - t * 105) * scale;
            float angle = age * .025f + seed + i * 1.7f;
            Ink(batch, ash, at, new Vector2(width * 1.1f, width * .55f), .08f * MathF.Sin(angle), 6, fade * .3f, Frame(ash, 192, seed + i));
            Ink(batch, veil, at, new Vector2(width * 1.2f, width * .45f), .08f * MathF.Sin(angle), 6, fade * .6f);
            Ink(batch, wind, at, new Vector2(width, width * .46f), angle, 6, fade * .8f);
        }
    }
    internal static void Brand(SpriteBatch batch, Vector2 at, float radius, float time, float opacity)
    {
        for (int i = 0; i < 2; i++) Ink(batch, arc, at, new Vector2(radius * 2, radius * .7f),
            i * MathHelper.Pi + MathF.Sin(time * 2) * .12f, 3, opacity * .7f);
        Head(batch, at - new Vector2(0, radius * .7f + 14), -Vector2.UnitY, CoreCasterKind.Nightmare, 8, time, 2, opacity * .8f);
    }
}
