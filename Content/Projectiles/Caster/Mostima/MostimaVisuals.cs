using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// These rawimg materials contain STRAIGHT alpha. Never use alpha-zero tints under AlphaBlend.
internal sealed class MostimaVisuals : IDisposable
{
    internal const string Assets = "ArknightsMod/Content/Projectiles/Caster/Mostima/Assets/";
    internal static readonly Color Blue = new(85, 175, 240);
    internal static readonly Color Violet = new(169, 125, 230);
    internal static MostimaVisuals Begin(BlendState blend = null)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, blend ?? BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        return new();
    }
    public void Dispose()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
    internal static void Sprite(string name, Vector2 center, Vector2 size, Color tint, float rotation = 0,
        SpriteEffects flip = SpriteEffects.None)
    {
        Texture2D texture = MostimaMaterials.Get(name);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), flip, 0);
    }
    internal static void Dial(Vector2 center, Vector2 size, float angle, Color tint)
    {
        Sprite("ChronoSigilV2", center, size, tint, angle);
    }
    internal static void Cross(Vector2 center, float diameter, float opacity, float angle = 0)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = angle + i * MathHelper.PiOver2;
            Sprite("ChronoHand", center + a.ToRotationVector2() * diameter * .15f,
                new Vector2(diameter * .52f, diameter * .22f), Blue * opacity, a);
        }
        // The filled diamond is only a small nucleus, never a full-screen opaque plate.
        Sprite("ChronoCore", center, new Vector2(diameter * .095f), new Color(205, 220, 255) * opacity);
    }
    internal static void Charge(Vector2 center, Vector2 target, float charge, float staffAngle, float time)
    {
        // 'time' is an integrated phase: speed decreases smoothly as charging approaches completion.
        DrawWarpedSmear(center, 50f + 78f * charge, time * 15f, .25f + charge * .35f);
        SpreadChargeSigil.DrawLayers(center, 65f + 105f * charge, staffAngle, -time,
            .35f + .65f * charge, Blue, MostimaMaterials.Get("ChronoSigilV2"));
        MostimaClockwork.Backplate(target, .5f + charge * .32f);
        using var pass = Begin();
        float size = 35f + 65f * charge;
        for (int ring = 0; ring < 2; ring++)
        {
            for (int i = 0; i < 8; i++)
            {
                float phase = time * (ring == 0 ? 1f : -.7f) + i * MathHelper.TwoPi / 8f;
                Vector2 offset = new Vector2(MathF.Cos(phase) * size * .20f,
                    MathF.Sin(phase) * size * .55f).RotatedBy(staffAngle);
                Sprite("TemporalStar", center + offset, new Vector2(7 + 7 * charge),
                    (ring == 0 ? Blue : Violet) * (.25f + .5f * charge), phase);
            }
        }
        Sprite("TemporalOrb", center, new Vector2(15f + 17f * charge),
            new Color(200, 238, 255) * (.30f + .30f * charge));
        // Preview and impact share the exact 280px collision radius and fixed orientation.
        // A faint complete boundary remains visible while the inner parts assemble.
        MostimaClockwork.Face(target, MostimaClockwork.Radius, time * 90f,
            .65f + charge * .35f, true);
    }
    internal static void Burst(Vector2 center, float age, float fade = 1f, float scale = 1f, float gold = 0f, float flowTime = 0f)
    {
        if (!SpreadVfxBudget.Visible(center, 680f * scale)) return;
        float face = MostimaClockwork.FaceOpacity(age) * fade;
        // Clean translucent glass supports the dial; all the motion lives in the
        // continuous stream system. Hands alone use the stepped attack timeline.
        MostimaClockwork.Backplate(center, .28f * face, scale, gold);
        MostimaGrandClockVfx.Draw(center, flowTime, face, scale, gold);
        using var pass = Begin();
        MostimaClockwork.Face(center, MostimaClockwork.Radius * scale, age, face * 1.65f, false, gold);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    private static float Pulse(float age, float center, float width)
        => MathHelper.Clamp(1f - MathF.Abs(age - center) / width, 0f, 1f);

    private static void DrawWarpedSmear(Vector2 center, float diameter, float age, float opacity)
    {
        Effect effect;
        try
        {
            effect = ModContent.Request<Effect>("ArknightsMod/Assets/Effects/MostimaTemporalWarp").Value;
        }
        catch (Exception)
        {
            effect = null;
        }
        if (effect == null || effect.Parameters["uTime"] == null || effect.Parameters["uTwist"] == null)
        {
            using var simple = Begin(BlendState.NonPremultiplied);
            Sprite("TemporalSmear", center, new Vector2(diameter),
                new Color(11, 27, 62) * (.45f * opacity), age * .068f);
            return;
        }
        effect.Parameters["uTime"].SetValue(age * .21f);
        effect.Parameters["uTwist"].SetValue(MathF.Sin(age * .06f) * .22f + .52f);
        Main.spriteBatch.End();
        bool open = false;
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                effect, Main.GameViewMatrix.TransformationMatrix);
            open = true;
            Sprite("TemporalSmear", center, new Vector2(diameter),
                new Color(11, 27, 62) * (.45f * opacity), age * .068f);
            Main.spriteBatch.End();
            open = false;
        }
        finally
        {
            if (open) Main.spriteBatch.End();
            if (Main.pixelShader?.CurrentTechnique != null)
                Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    private static void DrawChronoRibbons(Vector2 center, float age, float radius, float opacity)
    {
        for (int arm = 0; arm < 2; arm++)
        {
            Vector2[] history = new Vector2[27];
            float phase = age * (arm == 0 ? .16f : -.19f) + arm * MathHelper.Pi;
            Vector2 head = center + phase.ToRotationVector2() * radius;
            for (int i = 0; i < history.Length; i++)
            {
                float t = (i + 1f) / history.Length;
                float a = phase - (arm == 0 ? 1f : -1f) * t * 1.6f;
                history[i] = center + a.ToRotationVector2() * MathHelper.Lerp(radius, radius * .22f, t);
            }
            OperatorFlightVfx.Trail(head, history, Vector2.Zero,
                (arm == 0 ? Blue : Violet) * (opacity * .75f),
                new Color(225, 240, 255) * (opacity * .85f), 16f, 340f);
        }
    }
    internal static void Particles(Vector2 center, int count, float speed)
    {
        if (!SpreadVfxBudget.Visible(center, 100f) || ParticleManager.activeParticles.Count > 1300) return;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.1f, .1f);
            new T_Chrono_Particle(center, angle.ToRotationVector2() * speed * Main.rand.NextFloat(.65f, 1.2f),
                i % 2 == 0 ? Blue : Violet, Main.rand.Next(22, 38)).Spawn();
        }
    }
}



// Moving points on the two clock tracks, independent from the static dial texture.
