using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

// Original pixels with duplicated clamp borders, never resized or recolored.
// Pack ship 81 and Cinder Testament's closed sprite sets; retain all source pixels.
internal sealed class ReaperSwarmAtlas
{
    private const string Prefix = "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/";
    private static readonly string[] names = {
        "1cec3eaa9e3c2c71", "b0cf272039f6c1c1", "592845575d90ca97", "6bb096a3a44cb119",
        "77fa78a3dd9fba65", "b37f73047998fef4", "d379ea5eb5fb4a14", "1ccf4e4d7b0ce8f9",
        "419699a1ea1217d3", "3bdefa2bad6b1527", "be88b27326b45825", "76ed4690a36b95e3",
        "075157bbd3f28123", "83044062340edc16", "293a1fe38c743001", "217ca9f854c8dbc5"
    };
    private static readonly string[] cinderAssets = {
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/BossRewards/71cf4ee6ca83a3c8",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/BossRewards/79a40ab602656adc",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/BossRewards/e577b6fd6e4f7edb",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/0baa4f678f8653f0",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/1bf6f8c2c3fc4cf8",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/2e4847ec975c7059",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/4ac95336612b40c0",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/5af91e9a2e6abb4e",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/6bb096a3a44cb119",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/76ed4690a36b95e3",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/86aa778392b078a4",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/90c45048f404e49b",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/94369a78b53c1bec",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/a3f7e7354abc336c",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/b0cf272039f6c1c1",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/b41e449811dd3217",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/b70b34d65ef6d47e",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/be88b27326b45825",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/fa55488c54292467",
        "ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/Sprites/fef0b4d45ae17ebd",
    };
    private static readonly HashSet<string> eligibleAssets = EligibleAssets();
    private static HashSet<string> EligibleAssets() {
        var assets = new HashSet<string>(cinderAssets, StringComparer.Ordinal);
        foreach (string name in names) assets.Add(Prefix + name);
        return assets;
    }
    private readonly Dictionary<string, Rectangle> regions = new(StringComparer.Ordinal);
    private Texture2D texture;
    private bool attempted;
    internal Texture2D Texture => texture;
    internal void Prepare(GraphicsDevice device, Func<string, Texture2D> read)
    {
        if (attempted) return;
        attempted = true; Build(device, read);
    }
    internal bool TryGet(string asset, GraphicsDevice device, Func<string, Texture2D> read, out Vector2 offset, out Vector2 scale)
    {
        offset = Vector2.Zero; scale = Vector2.One;
        if (!eligibleAssets.Contains(asset)) return false;
        Prepare(device, read);
        if (texture == null || !regions.TryGetValue(asset, out Rectangle region)) return false;
        offset = new Vector2(region.X / (float)texture.Width, region.Y / (float)texture.Height);
        scale = new Vector2(region.Width / (float)texture.Width, region.Height / (float)texture.Height);
        return true;
    }
    private void Build(GraphicsDevice device, Func<string, Texture2D> read)
    {
        var entries = new List<(string Asset, Texture2D Source)>();
        int width = 1024;
        foreach (string asset in eligibleAssets) {
            Texture2D source = read(asset);
            while (width < source.Width + 4) width *= 2;
            entries.Add((asset, source));
        }
        entries.Sort((a, b) => b.Source.Height.CompareTo(a.Source.Height));
        int x = 0, y = 0, rowHeight = 0;
        foreach (var entry in entries) {
            if (x + entry.Source.Width + 4 > width) { x = 0; y += rowHeight; rowHeight = 0; }
            regions.Add(entry.Asset, new Rectangle(x + 2, y + 2, entry.Source.Width, entry.Source.Height));
            x += entry.Source.Width + 4;
            rowHeight = Math.Max(rowHeight, entry.Source.Height + 4);
        }
        int height = 1; while (height < y + rowHeight) height *= 2;
        var pixels = new Color[width * height];
        foreach (var entry in entries) {
            Texture2D source = entry.Source;
            var data = new Color[source.Width * source.Height]; source.GetData(data);
            Rectangle region = regions[entry.Asset];
            for (int py = -2; py < source.Height + 2; py++)
                for (int px = -2; px < source.Width + 2; px++)
                    pixels[(region.Y + py) * width + region.X + px] = data[Math.Clamp(py, 0, source.Height - 1) * source.Width + Math.Clamp(px, 0, source.Width - 1)];
        }
        texture = new Texture2D(device, width, height); texture.SetData(pixels);
    }
    internal Texture2D Release()
    {
        Texture2D result = texture; texture = null; regions.Clear(); attempted = false; return result;
    }
}
