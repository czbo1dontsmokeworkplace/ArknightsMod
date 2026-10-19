using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

/// <summary>Continuous procedural exhaust over actual flight history, with detached cooling embers.</summary>
internal static class ArtilleryFlightVisuals
{
    private static Effect shader;
    private static BlendState radiance;
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[74];
    private const float MaximumLength = 280f;

    private static bool Visible(Vector2 center) => !Main.dedServ && new Rectangle(
        (int)Main.screenPosition.X - 340, (int)Main.screenPosition.Y - 340,
        Main.screenWidth + 680, Main.screenHeight + 680).Contains(center.ToPoint());

    internal static void Emit(Projectile shell, Color outer, Color inner)
    {
        if (shell.numUpdates != 0 || !Visible(shell.Center) || ParticleManager.activeParticles.Count >= 1350)
            return;
        Vector2 direction = shell.velocity.SafeNormalize(Vector2.UnitY);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        Vector2 position = shell.Center - direction * Main.rand.NextFloat(12f, 30f);
        if (Main.rand.NextBool(2))
            new T_Ember_Particle(position + normal * Main.rand.NextFloat(-4f, 4f),
                -direction * Main.rand.NextFloat(.8f, 2.2f) + normal * Main.rand.NextFloat(-1.2f, 1.2f),
                Main.rand.Next(14, 23), outer, Main.rand.NextFloat(6f, 11f)).Spawn();
        new T_Streak_Particle(position, -direction * Main.rand.NextFloat(1f, 3f)
            + normal * Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.Next(12, 21),
            Main.rand.NextFloat(.14f, .24f), Color.Lerp(outer, inner, Main.rand.NextFloat()), true)
        { Deformation = new Vector2(.28f, 1.5f) }.Spawn();
    }

    // Used by the production renderer and offline GPU validation.
    internal static int BuildTrail(Projectile shell, float width)
    {
        Vector2 direction = shell.velocity.SafeNormalize(Vector2.UnitY);
        Vector2 previous = shell.Center;
        float distance = 0f;
        int count = 0;
        AddPair(previous, direction, 0f, width, ref count);
        for (int i = 0; i < shell.oldPos.Length && count + 2 <= vertices.Length; i++)
        {
            if (shell.oldPos[i] == Vector2.Zero) break;
            Vector2 point = shell.oldPos[i] + shell.Size * .5f;
            Vector2 delta = previous - point;
            float length = delta.Length();
            if (length > 96f) break; // A transfer/network correction must never produce a screen-wide ribbon.
            if (length < .1f) continue;
            bool last = distance + length >= MaximumLength;
            if (last) point = Vector2.Lerp(previous, point, (MaximumLength - distance) / length);
            distance = Math.Min(MaximumLength, distance + length);
            AddPair(point, delta / length, distance / MaximumLength, width, ref count);
            previous = point;
            if (last) break;
        }
        // Short/new histories also end in transparency instead of a rectangular cutoff.
        if (count >= 4)
        {
            vertices[count - 2].Color = Color.Transparent;
            vertices[count - 1].Color = Color.Transparent;
        }
        return count;
    }

    private static void AddPair(Vector2 center, Vector2 direction, float age, float width, ref int count)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        float radius = width * .5f * MathF.Pow(1f - age, .65f);
        Vector2 position = center - Main.screenPosition;
        Color alpha = Color.White * (.9f * MathF.Pow(1f - age, .5f));
        vertices[count++] = new VertexPositionColorTexture(new Vector3(position + normal * radius, 0), alpha, new(age, 0));
        vertices[count++] = new VertexPositionColorTexture(new Vector3(position - normal * radius, 0), alpha, new(age, 1));
    }

    internal static void Draw(Projectile shell, Color outer, Color inner, float width)
    {
        if (!Visible(shell.Center)) return;
        shader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ArtilleryFlightTrail", AssetRequestMode.ImmediateLoad).Value;
        radiance ??= new BlendState
        {
            ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One
        };
        int count = BuildTrail(shell, width);
        shader.Parameters["TrailTransform"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        shader.Parameters["OuterTint"].SetValue(outer.ToVector3());
        shader.Parameters["CoreTint"].SetValue(inner.ToVector3());
        shader.Parameters["FlowTime"].SetValue(Main.GlobalTimeWrappedHourly);
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, radiance, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (count >= 4)
            {
                shader.CurrentTechnique.Passes["FlamePass"].Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count - 2);
            }
            Main.spriteBatch.End();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D glow = TextureAssets.Extra[Terraria.ID.ExtrasID.ThePerfectGlow].Value;
            Vector2 exhaust = shell.Center - shell.velocity.SafeNormalize(Vector2.UnitY) * 13f;
            Main.spriteBatch.Draw(glow, exhaust - Main.screenPosition, null, inner * .48f,
                0f, glow.Size() * .5f, new Vector2(30f, 30f) / glow.Size(), SpriteEffects.None, 0f);
        }
        finally
        {
            Main.spriteBatch.End();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void Unload()
    {
        shader = null;
        radiance?.Dispose();
        radiance = null;
    }
}

public sealed class ArtilleryFlightVisualSystem : ModSystem
{
    public override void Unload() => ArtilleryFlightVisuals.Unload();
}
