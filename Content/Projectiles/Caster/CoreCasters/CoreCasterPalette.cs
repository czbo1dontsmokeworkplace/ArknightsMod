using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// The source has separate cool shell / pink ring / pale nucleus regions. Never collapse
// them with max(R,G,B): most saturated source pixels would become indistinguishable white.
internal static class CoreCasterPalette
{
    internal readonly record struct Colors(Color Shadow, Color Body, Color Ring, Color Core);
    private static readonly Dictionary<(Texture2D, int), Texture2D> materials = new();
    private static readonly Dictionary<int, Texture2D> bodies = new();
    private static Texture2D original;
    internal static int Key(CoreCasterKind kind, bool crimson = false) => crimson ? 7 : (int)kind;
    internal static Colors For(int key) => key switch {
        0 => new(new(17, 62, 105), new(42, 160, 218), new(109, 242, 229), new(233, 255, 248)),
        1 => new(new(23, 46, 110), new(65, 115, 217), new(161, 217, 255), new(243, 250, 255)),
        2 => new(new(36, 17, 74), new(97, 54, 164), new(197, 97, 205), new(245, 216, 244)),
        3 => new(new(47, 22, 93), new(139, 65, 181), new(240, 119, 181), new(255, 226, 237)),
        4 => new(new(15, 62, 58), new(24, 152, 99), new(152, 225, 83), new(240, 255, 193)),
        5 => new(new(111, 23, 20), new(228, 76, 18), new(255, 174, 27), new(255, 241, 151)),
        6 => new(new(15, 72, 78), new(53, 165, 153), new(139, 236, 174), new(232, 255, 220)),
        _ => new(new(86, 12, 38), new(183, 30, 80), new(255, 111, 144), new(255, 232, 227))
    };
    internal static void Assign(Texture2D source)
    {
        Unload(); original = source;
        // Prewarm eight tiny atlases; all read from the actual copied texture, not a replacement disk.
        for (int key = 0; key < 8; key++) bodies[key] = Grade(source, key, true);
    }
    internal static Texture2D Ball(int key) => bodies[key];
    internal static Texture2D Material(Texture2D source, int key)
    {
        var id = (source, key);
        if (!materials.TryGetValue(id, out Texture2D graded)) materials[id] = graded = Grade(source, key, false);
        return graded;
    }
    private static Texture2D Grade(Texture2D source, int key, bool coloredSource)
    {
        Colors p = For(key);
        var pixels = new Color[source.Width * source.Height]; source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++) {
            Color c = pixels[i]; if (c.A == 0) { pixels[i] = Color.Transparent; continue; }
            float a = c.A / 255f;
            float r = Math.Clamp(c.R / (255f * a), 0, 1), g = Math.Clamp(c.G / (255f * a), 0, 1), b = Math.Clamp(c.B / (255f * a), 0, 1);
            Color mapped;
            if (coloredSource) {
                float light = .2126f * r + .7152f * g + .0722f * b;
                if (Math.Min(r, Math.Min(g, b)) > .86f) mapped = p.Core;
                else if (r > g * 1.12f) {
                    mapped = Color.Lerp(p.Shadow, p.Ring, Math.Clamp((light - .12f) / .52f, 0, 1));
                    if (light > .78f) mapped = Color.Lerp(mapped, p.Core, (light - .78f) * 1.6f);
                }
                else mapped = Color.Lerp(p.Shadow, p.Body, Math.Clamp((light - .18f) / .61f, 0, 1));
            }
            else {
                // Most local masks store their grayscale in alpha, not RGB. Use that signal
                // for the palette ramp while retaining it as coverage for soft edges.
                float gray = a * (.2126f * r + .7152f * g + .0722f * b);
                // Some Calamity grayscale materials use opaque black backgrounds; others
                // store coverage in alpha. Combine BOTH channels so neither becomes a rectangle.
                a = gray;
                mapped = gray < .58f ? Color.Lerp(p.Shadow, p.Body, gray / .58f)
                    : Color.Lerp(p.Body, p.Ring, MathF.Pow((gray - .58f) / .42f, 1.5f));
            }
            pixels[i] = new Color((byte)(mapped.R * a), (byte)(mapped.G * a), (byte)(mapped.B * a), (byte)MathF.Round(255 * a));
        }
        var result = new Texture2D(source.GraphicsDevice, source.Width, source.Height); result.SetData(pixels); return result;
    }
    internal static void Unload()
    {
        foreach (var texture in bodies.Values) texture.Dispose(); bodies.Clear();
        foreach (var texture in materials.Values) texture.Dispose(); materials.Clear(); original = null;
    }
}
