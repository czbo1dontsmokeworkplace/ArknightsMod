using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// Image generation can leave tiny chromatic fringes in semitransparent pixels.
// Normalize the two new source masks to grayscale once; all visible color comes from draw tint.
internal sealed class MostimaMaterials : ModSystem
{
    private static readonly Dictionary<string, Texture2D> masks = new();
    private static readonly Dictionary<string, Texture2D> sources = new();
    internal static Texture2D Get(string name)
    {
        if (!sources.TryGetValue(name, out Texture2D source))
            sources[name] = source = ModContent.Request<Texture2D>(MostimaVisuals.Assets + name).Value;
        if (name is not ("ChronoSigilV2" or "ChronoCrescentV2")) return source;
        if (masks.TryGetValue(name, out Texture2D result)) return result;
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            byte luminance = (byte)((c.R * 54 + c.G * 183 + c.B * 19) / 256);
            pixels[i] = new Color(luminance, luminance, luminance, c.A);
        }
        result = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height);
        result.SetData(pixels);
        masks.Add(name, result);
        return result;
    }
    public override void Unload()
    {
        Texture2D[] textures = new Texture2D[masks.Count];
        masks.Values.CopyTo(textures, 0);
        masks.Clear();
        sources.Clear();
        Main.QueueMainThreadAction(() => { foreach (Texture2D texture in textures) texture.Dispose(); });
    }
}
