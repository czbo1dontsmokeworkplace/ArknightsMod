using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.SpreadArts;

// Image generation can leave chromatic fringes. Enforce neutral masks before tinting.
internal sealed class SpreadArtsMaterials : ModSystem
{
    internal const string Root = "ArknightsMod/Content/Textures/SpreadArts/";
    private static readonly Dictionary<string, Texture2D> masks = new();

    internal static Texture2D Get(string name)
    {
        if (masks.TryGetValue(name, out Texture2D cached)) return cached;
        Texture2D source = ModContent.Request<Texture2D>(Root + name).Value;
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            byte gray = (byte)((pixel.R * 54 + pixel.G * 183 + pixel.B * 19) / 256);
            pixels[i] = new Color(gray, gray, gray, pixel.A);
        }
        Texture2D mask = new(Main.instance.GraphicsDevice, source.Width, source.Height);
        mask.SetData(pixels);
        masks.Add(name, mask);
        return mask;
    }

    public override void Unload()
    {
        Texture2D[] textures = new Texture2D[masks.Count];
        masks.Values.CopyTo(textures, 0);
        masks.Clear();
        Main.QueueMainThreadAction(() =>
        {
            foreach (Texture2D texture in textures) texture.Dispose();
        });
    }
}
