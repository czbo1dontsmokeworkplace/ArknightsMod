using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// Shared accent textures; the arrow itself always retains a recognisable shaft.
internal enum CrossbowMaterial { Glint, Flux, Smear, SoftSpark }

internal static class CrossbowMaterials
{
    private readonly record struct Material(Texture2D Texture, Rectangle Crop, float Rotation);
    private static readonly Dictionary<CrossbowMaterial, Material> materials = new();
    internal static void Clear() => materials.Clear();
    internal static void Load()
    {
        if (Main.dedServ || materials.Count != 0) return;
        Add(CrossbowMaterial.Glint, "Content/Projectiles/Sniper/Typhon/Effects/Cross", new(0, 0, 64, 64));
        Add(CrossbowMaterial.Flux, "Common/Particle/DefaultParticle", new(0, 0, 64, 64));
        Add(CrossbowMaterial.Smear, "Content/Projectiles/Sniper/Schwarz/Assets/SchwarzSmear", new(80, 110, 1830, 580));
        Texture2D soft = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/MousseLightSplinter", AssetRequestMode.ImmediateLoad).Value;
        materials[CrossbowMaterial.SoftSpark] = new(soft, soft.Bounds, 0f);
    }
    private static void Add(CrossbowMaterial name, string path, Rectangle crop, float rotation = 0f)
        => materials[name] = new(ModContent.Request<Texture2D>("ArknightsMod/" + path, AssetRequestMode.ImmediateLoad).Value, crop, rotation);

    internal static void Draw(CrossbowMaterial name, Vector2 center, Vector2 size, Color tint,
        float rotation, float opacity, SpriteEffects flip = SpriteEffects.None)
    {
        if (opacity <= .001f || size.X <= 0f || size.Y <= 0f) return;
        Load();
        Material m = materials[name];
        if (m.Rotation != 0f) size = new Vector2(size.Y, size.X);
        Color color = new(tint.R, tint.G, tint.B, (byte)(MathHelper.Clamp(opacity, 0f, 1f) * tint.A));
        Main.spriteBatch.Draw(m.Texture, center - Main.screenPosition, m.Crop, color, rotation + m.Rotation,
            m.Crop.Size() * .5f, size / m.Crop.Size(), flip, 0f);
    }
}

// Arrow-attached accents get two explicit passes. Free particles use ParticleManager's blend grouping.
internal sealed class CrossbowMaterialPass : IDisposable
{
    private readonly record struct Layer(CrossbowMaterial Art, Vector2 Position, Vector2 Size,
        Color Tint, float Rotation, float Opacity, bool Bright, SpriteEffects Flip);
    private readonly List<Layer> layers = new(32);
    internal void Add(CrossbowMaterial art, Vector2 position, Vector2 size, Color tint, float rotation,
        float opacity = 1f, bool bright = true, SpriteEffects flip = SpriteEffects.None)
        => layers.Add(new(art, position, size, tint, rotation, opacity, bright, flip));
    public void Dispose()
    {
        if (Main.dedServ || layers.Count == 0) return;
        Main.spriteBatch.End();
        bool open = false;
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, pass == 0 ? BlendState.NonPremultiplied : BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                open = true;
                foreach (Layer layer in layers)
                    if (layer.Bright == (pass == 1))
                        CrossbowMaterials.Draw(layer.Art, layer.Position, layer.Size, layer.Tint, layer.Rotation, layer.Opacity, layer.Flip);
                Main.spriteBatch.End(); open = false;
            }
        }
        finally
        {
            if (open) Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}

internal sealed class CrossbowMaterialSystem : ModSystem
{
    public override void Unload() => CrossbowMaterials.Clear();
}
