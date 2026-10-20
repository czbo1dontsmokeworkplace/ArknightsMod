using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionBroodOutlineVisuals
{
    private static readonly Dictionary<string, Texture2D> masks = new();
    internal static Texture2D Mask(string name, Texture2D source)
    {
        if (masks.TryGetValue(name, out Texture2D mask)) return mask;
        // 只读取原图透明度，夜晚也能给黑色甲壳画出完整轮廓。每种图仅生成一次。
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte alpha = pixels[i].A;
            pixels[i] = new Color(alpha, alpha, alpha, alpha);
        }
        mask = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        mask.SetData(pixels);
        masks.Add(name, mask);
        return mask;
    }
    internal static void Unload()
    {
        foreach (Texture2D mask in masks.Values) Main.QueueMainThreadAction(mask.Dispose);
        masks.Clear();
    }
}
