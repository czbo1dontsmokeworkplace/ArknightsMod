using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// Port of 1.4.5.8 StormLightningDrawer, including its actual shader and 3-component strip UVs.
// The extra UV components carry pixel width; 1.4.4 VertexStrip cannot represent that format.
internal static class GoldenglowLightningRenderer
{
    private const string Assets = "ArknightsMod/Assets/Effects/Goldenglow/";
    private static Asset<Effect> shader;
    private static Asset<Texture2D> stripTexture;
    private static Asset<Texture2D> impact;

    private struct LightningVertex : IVertexType
    {
        public Vector2 Position;
        public Color Color;
        public Vector3 TexCoord;
        private static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
        public VertexDeclaration VertexDeclaration => Declaration;
        public LightningVertex(Vector2 position, Color color, Vector3 uv)
        {
            Position = position;
            Color = color;
            TexCoord = uv;
        }
    }

    internal sealed class Ribbon
    {
        private readonly GoldenglowLightningGeometry geometry;
        private readonly LightningVertex[] vertices;

        internal Ribbon(GoldenglowLightningGeometry geometry)
        {
            this.geometry = geometry;
            int count = 0;
            foreach (var bolt in geometry.Bolts)
                count += (bolt.Points.Length - 1) * 6;
            vertices = new LightningVertex[count];
        }

        internal void Draw(Vector2 origin, float width, Color color, float progress, float power = 1f)
        {
            if (vertices.Length == 0 || Main.dedServ)
                return;
            shader ??= ModContent.Request<Effect>(Assets + "LightningStrikeShader", AssetRequestMode.ImmediateLoad);
            stripTexture ??= ModContent.Request<Texture2D>(Assets + "LightningStrip", AssetRequestMode.ImmediateLoad);
            Main.spriteBatch.End();
            GraphicsDevice device = Main.instance.GraphicsDevice;
            BlendState blend = device.BlendState;
            DepthStencilState depth = device.DepthStencilState;
            RasterizerState rasterizer = device.RasterizerState;
            SamplerState sampler = device.SamplerStates[0];
            Texture oldTexture = device.Textures[0];
            var buffers = device.GetVertexBuffers();
            IndexBuffer indices = device.Indices;
            try
            {
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
                device.SamplerStates[0] = SamplerState.LinearWrap;
                device.Textures[0] = stripTexture.Value;
                Effect effect = shader.Value;
                Matrix projection = Matrix.CreateOrthographicOffCenter(0f, device.Viewport.Width,
                    device.Viewport.Height, 0f, -1f, 1f);
                effect.Parameters["MatrixTransform"]?.SetValue(Main.GameViewMatrix.TransformationMatrix * projection);
                effect.Parameters["uColor"]?.SetValue(Vector3.One);
                effect.Parameters["uSecondaryColor"]?.SetValue(Vector3.One);
                effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["uImageSize0"]?.SetValue(stripTexture.Value.Size());
                effect.Parameters["uSourceRect"]?.SetValue(new Vector4(0f, 0f, 4f, 4f));
                float opacity = Remap(progress, 0.1f, 0.25f, 0.5f, 1f) *
                    Remap(progress, 0.25f, 0.75f, 1f, 0f);
                effect.Parameters["uOpacity"]?.SetValue(opacity);
                foreach (var bolt in geometry.Bolts)
                {
                    float strength = bolt.Depth == 0 ? 1f : 0.5f * MathF.Pow(0.8f, bolt.Depth - 1);
                    effect.Parameters["uSaturation"]?.SetValue(strength * power);
                    effect.CurrentTechnique.Passes["StormLightning"].Apply();
                    int count = Build(bolt, origin, width, color, progress);
                    if (count > 0)
                        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count / 3);
                }
            }
            finally
            {
                device.SetVertexBuffers(buffers);
                device.Indices = indices;
                device.Textures[0] = oldTexture;
                device.BlendState = blend;
                device.DepthStencilState = depth;
                device.RasterizerState = rasterizer;
                device.SamplerStates[0] = sampler;
                Main.pixelShader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        private int Build(GoldenglowLightningGeometry.Bolt bolt, Vector2 origin, float width,
            Color tint, float progress)
        {
            int cursor = 0;
            LightningVertex left = default, right = default;
            for (int i = 0; i < bolt.Points.Length; i++)
            {
                float along = i / (bolt.Points.Length - 1f);
                float p = MathHelper.Lerp(bolt.StartProgress, bolt.EndProgress, along);
                float wave = Wave(p, Remap(progress, 0f, 0.15f, 0f, 1f), 0f, 1f) *
                    Wave(p, Remap(progress, 0.25f, 1f, 0f, 1f), 1f, 0f);
                float halfWidth = width * Remap(p, 0.5f, 1f, 1f, 0.5f) *
                    Remap(progress, 0.5f, 1f, 1f, 0.5f);
                if (bolt.EndProgress < 1f)
                    halfWidth *= Remap(bolt.EndProgress - p, 0.1f, 0f, 1f, bolt.Depth == 0 ? 0.5f : 0f);
                Vector2 pos = origin + bolt.Points[i] - Main.screenPosition;
                Vector2 normal = -bolt.Normals[i] * halfWidth;
                Color color = tint * wave;
                LightningVertex a = new(pos + normal, color, new Vector3(along, halfWidth * 2f, halfWidth * 2f));
                LightningVertex b = new(pos - normal, color, new Vector3(along, 0f, halfWidth * 2f));
                if (i > 0)
                {
                    vertices[cursor++] = left;
                    vertices[cursor++] = right;
                    vertices[cursor++] = a;
                    vertices[cursor++] = right;
                    vertices[cursor++] = b;
                    vertices[cursor++] = a;
                }
                left = a;
                right = b;
            }
            return cursor;
        }
    }

    internal static void DrawFlare(Vector2 position, Color tint, float scale, float opacity)
    {
        impact ??= ModContent.Request<Texture2D>(Assets + "LightningImpact");
        Texture2D texture = impact.Value;
        Color glow = tint * opacity;
        glow.A = 0;
        Vector2 point = position - Main.screenPosition;
        Main.EntitySpriteDraw(texture, point, null, glow, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(texture, point, null, new Color(255, 255, 255, 0) * opacity,
            0f, texture.Size() * 0.5f, scale * 0.52f, SpriteEffects.None);
    }

    private static float Remap(float value, float from, float to, float low, float high) =>
        GoldenglowLightningGeometry.Remap(value, from, to, low, high);
    private static float Wave(float along, float progress, float from, float to) =>
        Remap(along, MathHelper.Lerp(-0.5f, 1f, progress), MathHelper.Lerp(0f, 1.5f, progress), to, from);

    internal static void Unload() { shader = null; stripTexture = null; impact = null; }
}
