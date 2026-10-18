using System;
using System.Runtime.InteropServices;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

internal static class OpticalPacketTrail
{
    private static Effect material;
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct OpticalVertex : IVertexType
    {
        public readonly Vector2 Position;
        public readonly Color Tint;
        public readonly Vector2 UV;
        public OpticalVertex(Vector2 at, Color tint, Vector2 uv) { Position = at; Tint = tint; UV = uv; }
        private static readonly VertexDeclaration Layout = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        public VertexDeclaration VertexDeclaration => Layout;
    }
    private static readonly OpticalVertex[] strip = new OpticalVertex[BlastCasterRules.OpticalHistory * 2];
    internal static void Unload() => material = null;
    internal static void Draw(Vector2[] history, int count, float phase, float halfWidth, float opacity)
    {
        count = Math.Min(count, BlastCasterRules.OpticalHistory);
        if (count < 2 || opacity <= 0 || !(BlastCasterVisuals.Visible(history[0])
            || BlastCasterVisuals.Visible(history[count / 2]) || BlastCasterVisuals.Visible(history[count - 1]))) return;
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/AphrissaOpticalFlow", AssetRequestMode.ImmediateLoad).Value;
        for (int i = 0; i < count; i++)
        {
            Vector2 axis = i == count - 1 ? history[i - 1] - history[i] : history[i] - history[i + 1];
            axis = axis.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-axis.Y, axis.X);
            float t = i / (float)(count - 1);
            float width = halfWidth * (.35f + .65f * MathF.Sin(MathHelper.Pi * (.12f + t * .88f)));
            Color color = BlastCasterRules.Spectrum(phase + t * .22f) * opacity;
            strip[i * 2] = new(history[i] + normal * width - Main.screenPosition, color, new Vector2(t, 0));
            strip[i * 2 + 1] = new(history[i] - normal * width - Main.screenPosition, color, new Vector2(t, 1));
        }
        material.Parameters["WorldView"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["FlowTime"].SetValue(Main.GlobalTimeWrappedHourly);
        material.Parameters["Phase"].SetValue(phase);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            material.CurrentTechnique.Passes["OpticalPass"].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, strip, 0, count * 2 - 2);
        }
        finally { BlastCasterVisuals.RestoreBatch(); }
    }
}
public sealed class BlastCasterVfxSystem : ModSystem
{
    public override void Unload()
    {
        OpticalPacketTrail.Unload();
        global::ArknightsMod.Common.Particle.T.T_BlastCasterEmber.Glow = null;
    }
}
