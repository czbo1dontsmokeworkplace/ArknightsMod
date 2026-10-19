using System;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// Independently authored soft bloom material, with layered body/core stamps rather
// than a ribbon or a laser line. Stamp positions come only from actual flight.
internal static class BlastCasterFluxTrail
{
    private static Effect material;
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[BlastCasterRules.OpticalHistory * 12];
    internal static void Unload() => material = null;
    private static void Quad(ref int count, Vector2 at, Vector2 direction, Vector2 size, Color tint, float opacity)
    {
        Vector2 x = direction * size.X * .5f, y = new Vector2(-direction.Y, direction.X) * size.Y * .5f;
        at -= Main.screenPosition;
        tint.A = (byte)(Math.Clamp(opacity, 0, 1) * 255);
        vertices[count++] = new(new Vector3(at - x - y, 0), tint, new Vector2(0, 0));
        vertices[count++] = new(new Vector3(at + x - y, 0), tint, new Vector2(1, 0));
        vertices[count++] = new(new Vector3(at - x + y, 0), tint, new Vector2(0, 1));
        vertices[count++] = new(new Vector3(at - x + y, 0), tint, new Vector2(0, 1));
        vertices[count++] = new(new Vector3(at + x - y, 0), tint, new Vector2(1, 0));
        vertices[count++] = new(new Vector3(at + x + y, 0), tint, new Vector2(1, 1));
    }
    internal static void Draw(Vector2[] history, Vector2[] axes, float[] births, int count, float clock, BlastCasterKind kind, float widthScale)
    {
        if (Main.dedServ || count == 0) return;
        int used = 0;
        float maturity = Math.Clamp(clock / 5f, 0, 1);
        Color body = Color.Lerp(BlastCasterRules.Tint(kind), BlastCasterRules.FluxAccent(kind), maturity * .55f);
        Color core = Color.Lerp(body, new Color(255, 248, 221), .6f);
        float scale = BlastCasterRules.FluxScale * widthScale;
        // Match light per travelled distance across the three original cadences.
        // Slow, closely spaced packets otherwise pile up into a white rectangle.
        float density = Math.Clamp(BlastCasterRules.Step(kind) * BlastCasterRules.Updates(kind) / 72f
            * BlastCasterRules.Interval(kind) / 5f, .2f, 1.05f);
        for (int i = count - 1; i >= 0; i--)
        {
            float age = clock - births[i];
            if (age < 0 || age >= BlastCasterRules.FluxAfterglow || !BlastCasterVisuals.Visible(history[i])) continue;
            Vector2 axis = axes[i];
            float fade = 1 - MathF.Pow(age / BlastCasterRules.FluxAfterglow, 3);
            float shrink = MathF.Pow(.95f, age);
            Vector2 bodySize = new Vector2(MathHelper.Lerp(104, 98, maturity) * MathF.Pow(1.12f, age),
                MathHelper.Lerp(143, 49, maturity) * MathF.Pow(.88f, age)) * (scale * shrink);
            Vector2 coreSize = new Vector2(MathHelper.Lerp(98, 56, maturity) * MathF.Pow(1.12f, age),
                MathHelper.Lerp(49, 28, maturity) * MathF.Pow(.88f, age)) * (scale * shrink);
            Vector2 at = history[i] - axis * (age * 1.7f);
            Quad(ref used, at, axis, bodySize, body, fade * .40f * density);
            if (age < 5) Quad(ref used, at, axis, coreSize, core, (1 - MathF.Pow(age / 5f, 3)) * .46f * density);
        }
        if (used == 0) return;
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/BlastCasterFluxBloom", AssetRequestMode.ImmediateLoad).Value;
        material.Parameters["WorldView"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["FlowTime"].SetValue(Main.GlobalTimeWrappedHourly);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            material.CurrentTechnique.Passes["FluxBloom"].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);
        }
        finally { BlastCasterVisuals.RestoreBatch(); }
    }
}

public sealed class BlastCasterVfxSystem : ModSystem
{
    public override void Unload()
    {
        BlastCasterFluxTrail.Unload();
        global::ArknightsMod.Common.Particle.T.T_BlastCasterEmber.Glow = null;
    }
}
