using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// A status marker, not an attack area: all sizes below are final world pixels.
internal static class ExecutorStatusVisuals
{
    internal static readonly Color Gravel = new(190, 186, 202);
    internal static readonly Color Red = new(205, 48, 82);
    internal static readonly Color Phantom = new(145, 193, 225);

    internal static void Emit(Projectile carrier, int age, Color tint, float strength)
    {
        if (Main.dedServ || Main.gameMenu || strength <= 0f || age % 8 != 0) return;
        // One short-lived mote per eight ticks: at most three per carrier.
        new T_Executor_Rune(carrier, age * .21f, tint, strength).Spawn();
    }

    internal static void Draw(Vector2 center, int age, Color tint, float strength, float gravity = 1f)
    {
        if (Main.dedServ || !float.IsFinite(strength) || strength <= 0f) return;
        float fade = MathHelper.Clamp(strength, 0f, 1f);
        float breath = .94f + .06f * MathF.Sin(age * .045f);
        Texture2D ring = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Textures/SpreadArts/CasterOrbitSigil").Value;
        Texture2D core = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Textures/SpreadArts/CasterCoreSigil").Value;
        Vector2 position = center + new Vector2(0f, 17f * gravity) - Main.screenPosition;

        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            // Nonzero alpha lets the source texture's transparency control its light.
            Main.spriteBatch.Draw(ring, position, null, tint * (fade * .34f), 0f, ring.Size() * .5f,
                new Vector2(62f, 24f) * breath / ring.Size(), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(core, position, null, tint * (fade * .16f), 0f, core.Size() * .5f,
                new Vector2(38f, 15f) * breath / core.Size(), SpriteEffects.None, 0f);
        }
        finally
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
