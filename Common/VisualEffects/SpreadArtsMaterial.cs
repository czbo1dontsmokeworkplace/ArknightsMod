using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Only the three charged Arts weapons use this pass. Other approved weapon lines keep their renderer.
internal sealed class SpreadArtsMaterial : IDisposable
{
    private record struct Layer(string Name, Vector2 Center, Vector2 Size, Color Tint, float Angle);
    private static readonly List<Layer> Layers = new(40);
    private static readonly Dictionary<string, Texture2D> textures = new();
    internal static void Unload() { Layers.Clear(); textures.Clear(); }
    internal static SpreadArtsMaterial Begin() { Layers.Clear(); return new(); }
    internal static void Sprite(string name, Vector2 center, Vector2 size, Color tint, float angle = 0f)
    {
        if (!Main.dedServ && OperatorFlightVfx.Finite(center) && OperatorFlightVfx.Finite(size))
            Layers.Add(new(name, center, size, tint, angle));
    }
    public void Dispose()
    {
        if (Main.dedServ || Layers.Count == 0) return;
        Main.spriteBatch.End();
        bool batchOpen = false;
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                // rawimg contains straight RGBA; both passes must apply texture alpha to RGB.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, pass == 0 ? BlendState.NonPremultiplied : BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                batchOpen = true;
                foreach (Layer layer in Layers)
                {
                    bool emissive = layer.Tint.A == 0;
                    if (emissive != (pass == 1)) continue;
                    string path = layer.Name switch {
                        "BlastBloom" => "ArknightsMod/Content/Projectiles/Sniper/Fiammetta/Assets/FiammettaExplosionBloom",
                        "ChargeBloom" => "ArknightsMod/Common/Particle/DefaultParticle",
                        _ => OperatorFlightVfx.Assets + layer.Name };
                    if (!textures.TryGetValue(layer.Name, out Texture2D texture))
                        textures[layer.Name] = texture = ModContent.Request<Texture2D>(path).Value;
                    // In a real additive pass alpha must be nonzero. RGB already contains layer intensity.
                    Color color = emissive ? new Color(layer.Tint.R, layer.Tint.G, layer.Tint.B, 255) : layer.Tint * .42f;
                    Main.spriteBatch.Draw(texture, layer.Center - Main.screenPosition, null, color, layer.Angle,
                        texture.Size() * .5f, layer.Size / texture.Size(), SpriteEffects.None, 0f);
                }
                Main.spriteBatch.End();
                batchOpen = false;
            }
        }
        finally
        {
            if (batchOpen) Main.spriteBatch.End();
            Layers.Clear();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}

internal sealed class SpreadArtsMaterialSystem : ModSystem
{
    public override void Unload() => SpreadArtsMaterial.Unload();
}
