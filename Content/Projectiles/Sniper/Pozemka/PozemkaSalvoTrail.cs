using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Pozemka;

// SHPCRight_Proj's exact ImpFlameTrail material and two streak textures, loaded locally.
// Float3 UVs are required by that shader: z corrects trail-edge distortion.
internal static class PozemkaSalvoTrail
{
    private static Effect shader;
    private struct TrailVertex : IVertexType
    {
        public Vector3 Position;
        public Color Color;
        public Vector3 UV;
        public static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));
        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }
    internal static void Unload()
    {
        // Mod unload can run on a worker thread. Detach this generation immediately,
        // then release only its owned clone on the graphics thread.
        Effect oldShader = shader;
        shader = null;
        if (oldShader != null)
            Main.QueueMainThreadAction(() => oldShader.Dispose());
    }
    internal static void Draw(ReadOnlySpan<Vector2> history, float opacity)
    {
        if (Main.dedServ || history.Length < 2) return;
        shader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/Pozemka/ImpFlameTrail",
            AssetRequestMode.ImmediateLoad).Value.Clone();
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Main.spriteBatch.End();
        BlendState blend = device.BlendState;
        RasterizerState raster = device.RasterizerState;
        DepthStencilState depth = device.DepthStencilState;
        SamplerState sampler = device.SamplerStates[1];
        Texture texture = device.Textures[1];
        try
        {
            device.BlendState = BlendState.Additive;
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.None;
            device.SamplerStates[1] = SamplerState.LinearWrap;
            shader.Parameters["uWorldViewProjection"].SetValue(Main.GameViewMatrix.TransformationMatrix
                * Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1f));
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            Layer(device, history, 6f, opacity * .72f, "ScarletDevilStreak", false);
            Layer(device, history.Slice(0, Math.Min(9, history.Length)), 2.8f, opacity * .8f,
                "SylvestaffStreak", true);
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            device.BlendState = blend;
            device.RasterizerState = raster;
            device.DepthStencilState = depth;
            device.SamplerStates[1] = sampler;
            device.Textures[1] = texture;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    private static void Layer(GraphicsDevice device, ReadOnlySpan<Vector2> path, float width,
        float opacity, string texture, bool core)
    {
        int count = path.Length;
        if (count < 2) return;
        var vertices = new TrailVertex[count * 2];
        var indices = new short[(count - 1) * 6];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector2 tangent = (path[Math.Max(0, i - 1)] - path[Math.Min(count - 1, i + 1)]).SafeNormalize(Vector2.UnitX);
            Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);
            float w = t < .18f ? MathF.Sin(t / .18f * MathHelper.PiOver2) * width + .18f
                : MathHelper.Lerp(width, 0f, (t - .18f) / .82f);
            Color color = core ? Color.Lerp(Color.White, new Color(255, 213, 232), t * .5f)
                : Color.Lerp(new Color(255, 164, 207), new Color(198, 62, 126), t * .65f);
            color *= opacity * (1f - t * Utils.GetLerpValue(core ? .78f : .74f, 1f, t, true));
            Vector2 center = path[i] - Main.screenPosition;
            vertices[i * 2] = new() { Position = new Vector3(center + normal * w, 0f), Color = color, UV = new(t, 0f, 1f) };
            vertices[i * 2 + 1] = new() { Position = new Vector3(center - normal * w, 0f), Color = color, UV = new(t, 1f, 1f) };
            if (i == count - 1) continue;
            int k = i * 6;
            indices[k] = (short)(i * 2); indices[k + 1] = (short)(i * 2 + 1); indices[k + 2] = (short)(i * 2 + 2);
            indices[k + 3] = (short)(i * 2 + 1); indices[k + 4] = (short)(i * 2 + 3); indices[k + 5] = (short)(i * 2 + 2);
        }
        device.Textures[1] = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/Pozemka/" + texture,
            AssetRequestMode.ImmediateLoad).Value;
        shader.CurrentTechnique.Passes["TrailPass"].Apply();
        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length,
            indices, 0, indices.Length / 3);
    }
}

public sealed class PozemkaSalvoTrailSystem : ModSystem
{
    public override void Unload() => PozemkaSalvoTrail.Unload();
}
