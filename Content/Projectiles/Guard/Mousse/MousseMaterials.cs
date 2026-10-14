using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

// The painted masks are kept neutral at draw time, then tinted by the weapon.
internal sealed class MousseMaterials : ModSystem
{
    internal const string Root = "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/";
    private static readonly Dictionary<string, Texture2D> Masks = new();
    private static readonly Dictionary<string, Texture2D> StraightMasks = new();
    private static readonly Dictionary<string, Rectangle> VisibleBounds = new();

    internal static Rectangle GetVisibleBounds(string name)
    {
        if (VisibleBounds.TryGetValue(name, out Rectangle bounds)) return bounds;
        Texture2D texture = Get(name);
        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        int left = texture.Width, top = texture.Height, right = -1, bottom = -1;
        for (int y = 0; y < texture.Height; y++)
            for (int x = 0; x < texture.Width; x++)
                if (pixels[y * texture.Width + x].R > 4)
                { left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
        bounds = right < left ? new Rectangle(0, 0, texture.Width, texture.Height)
            : new Rectangle(left, top, right - left + 1, bottom - top + 1);
        VisibleBounds.Add(name, bounds);
        return bounds;
    }

    internal static Texture2D GetStraight(string name)
    {
        if (StraightMasks.TryGetValue(name, out Texture2D cached)) return cached;
        Texture2D source = Get(name);
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            byte gray = c.A == 0 ? (byte)0 : (byte)Math.Min(255, c.R * 255 / c.A);
            pixels[i] = new Color(gray, gray, gray, c.A);
        }
        Texture2D result = new(Main.instance.GraphicsDevice, source.Width, source.Height);
        result.SetData(pixels);
        StraightMasks.Add(name, result);
        return result;
    }

    public override void PostSetupContent()
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() => {
            foreach (string name in new[] { "MousseEnergyColumn", "MousseCrescent", "MousseLightSplinter", "GlowOrb", "SolidOrb" })
                Get(name);
        });
    }

    internal static Texture2D Get(string name)
    {
        if (Masks.TryGetValue(name, out Texture2D cached)) return cached;
        if (name == "GlowOrb" || name == "SolidOrb")
        {
            const int size = 64;
            Color[] data = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float radius = new Vector2((x + .5f - size * .5f) / (size * .5f), (y + .5f - size * .5f) / (size * .5f)).Length();
                    float value = name == "SolidOrb" ? MathHelper.Clamp((1f - radius) * 12f, 0f, 1f)
                        : MathF.Pow(Math.Max(0f, 1f - radius * radius), 3f);
                    data[y * size + x] = Color.White * value;
                }
            Texture2D orb = new(Main.instance.GraphicsDevice, size, size);
            orb.SetData(data);
            Masks.Add(name, orb);
            return orb;
        }
        Texture2D original = ModContent.Request<Texture2D>(Root + name).Value;
        Color[] pixels = new Color[original.Width * original.Height];
        original.GetData(pixels);
        // Verified against this installation: ImageIO.ToRaw/RawImgReader preserve straight RGB;
        // ReLogic PngReader premultiplies. Normalize BOTH paths before AlphaBlend dust draws.
        bool straight = ModLoader.GetMod("ArknightsMod").FileExists(
            "Content/Projectiles/Guard/Mousse/Assets/" + name + ".rawimg");
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            byte luminance = (byte)((c.R * 54 + c.G * 183 + c.B * 19) / 256);
            if (straight) luminance = (byte)(luminance * c.A / 255);
            pixels[i] = new Color(luminance, luminance, luminance, c.A);
        }
        Texture2D neutral = new(Main.instance.GraphicsDevice, original.Width, original.Height);
        neutral.SetData(pixels);
        Masks.Add(name, neutral);
        return neutral;
    }

    public override void Unload()
    {
        Texture2D[] old = new Texture2D[Masks.Count];
        Masks.Values.CopyTo(old, 0);
        Masks.Clear();
        Texture2D[] oldStraight = new Texture2D[StraightMasks.Count];
        StraightMasks.Values.CopyTo(oldStraight, 0);
        StraightMasks.Clear();
        VisibleBounds.Clear();
        if (!Main.dedServ)
            Main.QueueMainThreadAction(() =>
            {
                foreach (Texture2D texture in old) texture.Dispose();
                foreach (Texture2D texture in oldStraight) texture.Dispose();
            });
    }
}
