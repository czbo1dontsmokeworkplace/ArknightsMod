using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// Calamity-style fast spell light: pointed colored shells, narrow pale cores and source-textured trails.
internal static class CoreCasterArt
{
    internal const string Assets = "ArknightsMod/Content/Textures/CoreCasters/";
    internal const int CrimsonPalette = 10;
    internal static readonly Color Crimson = new(183, 30, 80);
    private static Texture2D spark, bloom, arc, pressure, fire, ash, crystal, soul, veil, wind, needle, point, ring, circle, lance, energyStreak, shock;
    internal static bool Ready => lance != null && !lance.IsDisposed;
    internal static void Assign(Func<string, Texture2D> read)
    {
        spark = read("Spark"); bloom = read("GelBloomCircle"); ring = read("GelBloomRing");
        point = read("GelPoint"); circle = read("GelCircle"); arc = read("DreamArc"); pressure = read("Pressure");
        fire = read("FireTufts"); ash = read("AshCloud"); crystal = read("Crystal"); soul = read("Soul");
        lance = read("EnergyLance"); energyStreak = read("EnergyStreak"); shock = read("EnergyShock");
        veil = read("Veil"); wind = read("WindCrescent"); needle = read("TechNeedle");
    }
    internal static void Unload()
    {
        CoreCasterPalette.Unload(); spark = bloom = arc = pressure = fire = ash = crystal = soul = veil = wind = needle = point = ring = circle = lance = energyStreak = shock = null;
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
    private static Color Light(Color color, float strength) => new Color(color.R, color.G, color.B, 0) * Math.Clamp(strength, 0, 1);
    private static void Lance(SpriteBatch batch, Vector2 at, Vector2 direction, float length, float width, int key, float opacity)
    {
        var p = CoreCasterPalette.For(key);
        float angle = Angle(direction) + MathHelper.PiOver2;
        // IonBlast / WingmanShot: colored pointed shell, smaller pale core. No orbiting geometry.
        Sprite(batch, lance, at, new Vector2(width * 2.8f, length * 1.12f), angle, Light(p.Body, opacity * .35f));
        Sprite(batch, lance, at, new Vector2(width * 1.25f, length), angle, Light(p.Body, opacity));
        Sprite(batch, lance, at + Unit(direction) * length * .08f, new Vector2(width * .42f, length * .72f), angle,
            Light(Color.Lerp(p.Ring, p.Core, .62f), opacity * .95f));
    }
    internal static void Head(SpriteBatch batch, Vector2 at, Vector2 direction, CoreCasterKind kind, float size, float time, int seed,
        float opacity = 1, bool additive = false, Color? overrideColor = null)
    {
        int key = Key(kind, overrideColor); var p = CoreCasterPalette.For(key);
        Vector2 forward = Unit(direction);
        float length = size * (kind == CoreCasterKind.Absinthe ? 4.1f : kind == CoreCasterKind.Nightmare ? 3.5f : 3);
        float width = size * (kind == CoreCasterKind.Absinthe ? .62f : .85f);
        if (kind == CoreCasterKind.Eyjafjalla) {
            // Keep flame texture behind a sharply readable molten head.
            Flame(batch, at - forward * size, forward, size * 2.2f, .25f, seed + (int)(time * 24), opacity * .55f);
        }
        Lance(batch, at, forward, length, width, key, opacity);
    }
    internal static void Bolt(SpriteBatch batch, Vector2 center, Vector2 velocity, Vector2[] history, Vector2 offset,
        CoreCasterKind kind, int mode, float time, int seed)
    {
        int key = CoreCasterPalette.Key(kind, kind == CoreCasterKind.Haze && mode == 2);
        float size = kind switch { CoreCasterKind.Durin => 9, CoreCasterKind.Steward => 12,
            CoreCasterKind.Absinthe => 11, CoreCasterKind.Eyjafjalla => mode == 3 ? 13 : 15, _ => 14 };
        // Actual curved mesh owns the tail. Do not stack repeated heads, swirls or detached plates on it.
        Head(batch, center, velocity, kind, size * (mode > 0 ? 1.12f : 1), time, seed,
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
        if (variant == 2) { Burst(batch, at, kind, size, progress, seed, overrideColor: key == 7 ? Crimson : null); return; }
        if (variant == 3) { Flame(batch, at, velocity, size * (1 + progress * .35f), progress, seed, .7f); return; }
        if (variant == 1) {
            // Low coverage aftermath. It must not obscure the contact's bright split-second shape.
            Ink(batch, ash, at, new Vector2(size * (1 + progress * .7f)), seed * .7f + progress * .15f, key,
                MathF.Sin(progress * MathHelper.Pi) * .18f, Frame(ash, 192, seed));return;
        }
        if (variant == 7) {
            // Omicron / Genesis detached luminous streak, drifting independently from the live trail.
            float length = size * (1 - progress * .65f);
            Sprite(batch, energyStreak, at, new Vector2(size * .12f * (1 - progress), length), angle + MathHelper.PiOver2,
                Light(p.Body, fade * .7f));return;
        }
        if (variant == 8) {
            // Flattened source shock, aligned with the incoming projectile instead of a giant circular icon.
            float grow = 1 - MathF.Pow(1 - progress, 3);
            Sprite(batch, shock, at, new Vector2(size, size * .38f) * (.35f + grow), angle + MathHelper.PiOver2,
                Light(p.Ring, MathF.Pow(1 - progress, 2) * .65f), new Rectangle(36, 952, 2374, 781));return;
        }
        float sparkLength = size * (variant == 5 ? 1.5f : .8f) * (1 - progress * .65f);
        Lance(batch, at, velocity, sparkLength, Math.Max(1.2f, size * .12f) * (1 - progress), key, fade * .8f);
    }
    internal static void Burst(SpriteBatch batch, Vector2 at, CoreCasterKind kind, float radius, float progress, int seed, bool additive = false, Color? overrideColor = null)
    {
        int key = Key(kind, overrideColor); var p = CoreCasterPalette.For(key);
        progress = Math.Clamp(progress, 0, 1);
        float flash = Math.Max(0, 1 - progress * 5), grow = 1 - MathF.Pow(1 - progress, 3);
        float fade = MathF.Pow(1 - progress, 2.5f);
        // The hit is a fast bloom + sharp broken rays, followed by a much dimmer expanding shock.
        // Spatially sparse and temporally short: no lingering giant disk on every ordinary hit.
        Glow(batch, at, new Vector2(radius * 2.5f), p.Body, fade * .58f);
        Glow(batch, at, new Vector2(radius * .9f), p.Core, flash * .85f);
        for (int i = 0; i < 3; i++) {
            float angle = seed * .71f + i * 2.399963f;
            Lance(batch, at, Circle(angle), radius * (1.7f - i * .3f) * flash, radius * .11f * flash, key, flash * .8f);
        }
        if (kind == CoreCasterKind.Eyjafjalla) for (int i = 0; i < (radius > 70 ? 5 : 3); i++) {
            Vector2 drift = Circle(seed * .3f + i * 2.399963f) * new Vector2(.65f, .4f) * radius * grow * .6f;
            Flame(batch, at + drift, new Vector2(drift.X * .015f, -1), radius * (.6f + i % 3 * .07f), progress, seed + i, 1 - progress);
        }
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
