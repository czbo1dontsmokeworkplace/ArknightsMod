using System;
using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

internal static class StandardBearerCrescentMaterial
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[4];
    internal static void Unload() => material = null;
    internal static void Draw(Vector2 center, float angle, StandardBearerKind kind, float age)
    {
        if (Main.dedServ || age >= StandardBearerRules.WaveLife || age < 0) return;
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/StandardBearerCrescent", AssetRequestMode.ImmediateLoad).Value;
        Vector2 x = angle.ToRotationVector2() * 16, y = (angle + MathHelper.PiOver2).ToRotationVector2() * 16;
        center -= Main.screenPosition;
        quad[0] = new(new Vector3(center - x - y, 0), Color.White, new(0, 0));
        quad[1] = new(new Vector3(center + x - y, 0), Color.White, new(1, 0));
        quad[2] = new(new Vector3(center - x + y, 0), Color.White, new(0, 1));
        quad[3] = new(new Vector3(center + x + y, 0), Color.White, new(1, 1));
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["Tint"].SetValue(StandardBearerRules.Tint(kind).ToVector3());
        material.Parameters["Age"].SetValue(Math.Clamp(age / StandardBearerRules.WaveLife, 0, 1));
        var device = Main.spriteBatch.GraphicsDevice;
        BlendState blend = device.BlendState; DepthStencilState depth = device.DepthStencilState;
        RasterizerState raster = device.RasterizerState; SamplerState sampler = device.SamplerStates[0];
        var texture = device.Textures[0];
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, quad, 0, 2);
        }
        finally
        {
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.SamplerStates[0] = sampler; device.Textures[0] = texture;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
public sealed class StandardBearerCrescentVisualSystem : ModSystem
{
    public override void Unload() => StandardBearerCrescentMaterial.Unload();
}
