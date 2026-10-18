using System;
using System.Runtime.InteropServices;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

// Local arc geometry and an independently implemented procedural material.
// No third-party shader, compiled effect or noise texture is loaded.
internal static class DreadnoughtArcTrail
{
    private static Effect shader;
    private const int Samples = 96;
    private static readonly SlashVertex[] vertices = new SlashVertex[Samples * 2];

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct SlashVertex : IVertexType
    {
        public readonly Vector2 Position;
        public readonly Color Color;
        public readonly Vector2 UV;
        public SlashVertex(Vector2 position, Color color, Vector2 uv)
        { Position = position; Color = color; UV = uv; }
        public VertexDeclaration VertexDeclaration => Declaration;
        private static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
    }

    internal static void Unload() => shader = null;

    internal static void Draw(Vector2 center, float aimAngle, int facing, float progress, float reach, ChargedOperatorKind kind)
    {
        if (!ChargedOperatorVfx.Visible(center) || progress < .16f || progress >= 1) return;
        shader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/DreadnoughtArcTrail", AssetRequestMode.ImmediateLoad).Value;
        float tail = Math.Max(0, progress - .30f);
        float speed = DreadnoughtRules.TrailIntensity(progress);
        float alpha = (.35f + .65f * speed) * Math.Clamp((1 - progress) / .12f, 0, 1);
        for (int i = 0; i < Samples; i++)
        {
            float t = i / (float)(Samples - 1);
            float sample = MathHelper.Lerp(progress, tail, t);
            float angle = aimAngle + MathHelper.Lerp(-MathHelper.Pi, MathHelper.Pi, DreadnoughtRules.Turn(sample)) * facing;
            Vector2 axis = angle.ToRotationVector2();
            Color color = Color.White * (alpha * MathF.Pow(1 - t, .3f));
            // The bright outside edge ends at the visible blade tip, not 45 degrees
            // ahead of it. The inner edge is 40% of blade length from the grip.
            vertices[i * 2] = new SlashVertex(center + axis * reach - Main.screenPosition, color, new Vector2(t, 0));
            vertices[i * 2 + 1] = new SlashVertex(center + axis * reach * .4f - Main.screenPosition, color, new Vector2(t, 1));
        }

        (Color body, Color shadow, Color edge) = Palette(kind);
        shader.Parameters["BodyTint"].SetValue(body.ToVector3());
        shader.Parameters["ShadowTint"].SetValue(shadow.ToVector3());
        shader.Parameters["EdgeTint"].SetValue(edge.ToVector3());
        shader.Parameters["AnimationTime"].SetValue(Main.GlobalTimeWrappedHourly);
        shader.Parameters["ArcTransform"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));

        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            shader.CurrentTechnique.Passes["ArcPass"].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
        }
        finally
        {
            Main.spriteBatch.End();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    private static (Color Body, Color Shadow, Color Edge) Palette(ChargedOperatorKind kind) => kind switch
    {
        ChargedOperatorKind.Melantha => (new(202, 85, 155), new(40, 12, 45), new(255, 230, 249)),
        ChargedOperatorKind.Matoimaru => (new(104, 228, 187), new(12, 37, 48), new(224, 255, 240)),
        ChargedOperatorKind.Franka => (new(255, 118, 41), new(56, 13, 30), new(255, 246, 211)),
        _ => (new(255, 207, 85), new(62, 26, 44), new(255, 253, 230))
    };

}

public sealed class DreadnoughtVfxSystem : ModSystem
{
    public override void Unload() => DreadnoughtArcTrail.Unload();
}
