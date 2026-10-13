using System;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Two original grayscale sigils share one center. UVs are aimed, stretched on
// one axis and counter-rotated while the geometry itself stays fixed.
internal static class SpreadChargeSigil
{
    private static BasicEffect projectionEffect;
    private static readonly short[] QuadIndices = { 0, 1, 2, 0, 2, 3 };

    internal static void Draw(Vector2 center, float diameter, float facing, float time,
        float strength, Color color)
        => DrawLayers(center, diameter, facing,
            -time * MathHelper.ToRadians(5.6f) * 60f, strength, color);

    internal static void DrawLayers(Vector2 center, float diameter, float facing, float phase,
        float strength, Color color, Texture2D customSigil = null)
    {
        if (Main.dedServ || strength <= 0f) return;
        Texture2D outer = customSigil ?? SpreadArtsMaterials.Get("CasterOrbitSigil");
        Texture2D inner = customSigil ?? SpreadArtsMaterials.Get("CasterCoreSigil");
        if (outer == null || inner == null) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        projectionEffect ??= new BasicEffect(device)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        projectionEffect.World = Matrix.Identity;
        projectionEffect.View = Matrix.Identity;
        projectionEffect.Projection = Matrix.CreateOrthographicOffCenter(0f,
            device.Viewport.Width, device.Viewport.Height, 0f, 0f, 1f);

        float opening = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(strength, 0f, 1f));
        float size = diameter * (.45f + .55f * opening);
        Vector2 position = Vector2.Transform(center - Main.screenPosition,
            Main.GameViewMatrix.TransformationMatrix);
        Vector2 zoom = Main.GameViewMatrix.Zoom;

        Main.spriteBatch.End();
        try
        {
            DrawLayer(device, outer, position, size * 1.075f, zoom, facing, phase,
                color, opening * .26f, BlendState.Additive);
            DrawLayer(device, outer, position, size, zoom, facing, phase,
                color, opening * .75f, BlendState.NonPremultiplied);
            float innerSize = customSigil == null ? size : size * .58f;
            DrawLayer(device, inner, position, innerSize * 1.075f, zoom, facing, 0f,
                color, opening * .2f, BlendState.Additive);
            DrawLayer(device, inner, position, innerSize, zoom, facing, 0f,
                color, opening * .72f, BlendState.NonPremultiplied);
        }
        finally
        {
            if (Main.pixelShader?.CurrentTechnique != null)
                Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    private static void DrawLayer(GraphicsDevice device, Texture2D texture, Vector2 center,
        float diameter, Vector2 zoom, float facing, float phase, Color tint,
        float opacity, BlendState blend)
    {
        float halfWidth = diameter * zoom.X * .5f;
        float halfHeight = diameter * texture.Height / texture.Width * zoom.Y * .5f;
        Vector2[] corners =
        {
            new(center.X - halfWidth, center.Y - halfHeight),
            new(center.X + halfWidth, center.Y - halfHeight),
            new(center.X + halfWidth, center.Y + halfHeight),
            new(center.X - halfWidth, center.Y + halfHeight)
        };
        Vector2[] uv =
        {
            WarpUv(new Vector2(0f, 0f), facing, phase),
            WarpUv(new Vector2(1f, 0f), facing, phase),
            WarpUv(new Vector2(1f, 1f), facing, phase),
            WarpUv(new Vector2(0f, 1f), facing, phase)
        };
        Color vertexColor = tint * opacity;
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new VertexPositionColorTexture(
                new Vector3(corners[i], 0f), vertexColor, uv[i]);

        device.BlendState = blend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearClamp;
        projectionEffect.Texture = texture;
        foreach (EffectPass pass in projectionEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                vertices, 0, vertices.Length, QuadIndices, 0, 2);
        }
    }

    private static Vector2 WarpUv(Vector2 uv, float aim, float phase)
    {
        Vector2 offset = uv - new Vector2(.5f);
        offset = RotateSample(offset, aim);
        offset.X *= 2f;
        offset = RotateSample(offset, phase);
        uv = offset + new Vector2(.5f);
        if (MathF.Cos(aim) < 0f) uv.Y = 1f - uv.Y;
        return uv;
    }

    private static Vector2 RotateSample(Vector2 value, float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(value.X * cosine + value.Y * sine,
            -value.X * sine + value.Y * cosine);
    }

    internal static void Unload()
    {
        BasicEffect old = projectionEffect;
        projectionEffect = null;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
}

internal sealed class SpreadChargeSigilSystem : ModSystem
{
    public override void Unload() => SpreadChargeSigil.Unload();
}
