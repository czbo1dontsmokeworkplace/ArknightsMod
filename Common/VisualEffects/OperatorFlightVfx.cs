using ArknightsMod.Common.Particle.T;
using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Local material library: copied from the author's Fran artwork. No external mod lookup.
internal static class OperatorFlightVfx
{
    internal const string Assets = "ArknightsMod/Content/Textures/OperatorVfx/";
    private static MiscShaderData ribbonShader;
    internal static void Unload() => ribbonShader = null;
    private static MiscShaderData RibbonShader
    {
        get
        {
            if (ribbonShader != null) return ribbonShader;
            // Match the installed Terraria vertex shader, but own the parameter state. Never mutate
            // GameShaders.Misc["FlameLash"], which other weapons also use in the same frame.
#pragma warning disable CS0618
            ribbonShader = new MiscShaderData(Main.VertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(true);
#pragma warning restore CS0618
            ribbonShader.UseImage1("Images/Extra_189");
            ribbonShader.UseImage2("Images/Extra_190");
            return ribbonShader;
        }
    }
    internal static Color Add(Color tint, float opacity = 1f) => new Color(tint.R, tint.G, tint.B, 0) * opacity;
    internal static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
    internal static void Sprite(string name, Vector2 center, Vector2 size, Color tint, float angle = 0f)
    {
        if (Main.dedServ || !Finite(center) || !Finite(size)) return;
        Texture2D texture = ModContent.Request<Texture2D>(Assets + name).Value;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, tint, angle,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }

    internal static void Trail(Projectile p, Color body, Color tip, float width, float length)
        => Trail(p.Center, p.oldPos, p.Size * .5f, body, tip, width, length);

    // Crop by world distance BEFORE rendering. Extra updates cannot stretch a tail across the screen.
    internal static void Trail(Vector2 head, Vector2[] history, Vector2 offset,
        Color body, Color tip, float width, float length)
    {
        if (Main.dedServ || !Finite(head)) return;
        length = MathHelper.Clamp(length, 32f, 360f);
        List<Vector2> path = new(34) { head };
        float total = 0f;
        foreach (Vector2 saved in history)
        {
            if (saved == Vector2.Zero || !Finite(saved)) break;
            Vector2 next = saved + offset;
            float step = Vector2.Distance(path[^1], next);
            if (step > 96f) break; // teleport/discontinuous history
            if (step < .5f) continue;
            float remaining = length - total;
            path.Add(step > remaining ? Vector2.Lerp(path[^1], next, remaining / step) : next);
            total += Math.Min(step, remaining);
            if (total >= length || path.Count >= 34) break;
        }
        if (path.Count < 3 || total < 4f) return;
        // Uniform arc-length samples keep the envelope stable during acceleration and curved flight.
        const int count = 32;
        Vector2[] points = new Vector2[count];
        float[] rotations = new float[count];
        int segment = 1;
        float consumed = 0f;
        for (int i = 0; i < count; i++)
        {
            float distance = total * i / (count - 1f);
            float span = Vector2.Distance(path[segment - 1], path[segment]);
            while (segment < path.Count - 1 && consumed + span < distance)
            {
                consumed += span;
                segment++;
                span = Vector2.Distance(path[segment - 1], path[segment]);
            }
            points[i] = Vector2.Lerp(path[segment - 1], path[segment], MathHelper.Clamp((distance - consumed) / Math.Max(.001f, span), 0f, 1f));
            rotations[i] = (path[segment - 1] - path[segment]).ToRotation();
        }
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            MiscShaderData shader = RibbonShader;
            // String overload resolves vanilla Main.Assets; mod textures require the Asset overload.
            shader.UseImage0(ModContent.Request<Texture2D>(Assets + "TrailFilament"));
            shader.UseColor(body);
            shader.UseSecondaryColor(tip);
            shader.UseOpacity(.65f);
            shader.UseSaturation(0f);
            shader.Apply();
            VertexStrip strip = new();
            strip.PrepareStrip(points, rotations,
                t => Color.Lerp(tip, body, t) * MathF.Pow(1f - t, 1.15f),
                t => MathHelper.Clamp(width, 1f, 34f) * (.32f + .68f * MathF.Sin(MathHelper.Pi * MathF.Pow(t, .7f))) * (1f - t),
                -Main.screenPosition);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void Spark(Vector2 position, Vector2 velocity, Color color, float length = 20f, int life = 18, float width = 3f)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1300 || !Finite(position)) return;
        new T_LinearSpark_Particle(position, velocity, color, length, life, width).Spawn();
    }
    internal static void Ring(Vector2 position, Color color, float size, int life = 20)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1300) return;
        new T_Wave_Particle(position, color, size, life).Spawn();
    }
    internal static void Flame(Vector2 position, Vector2 velocity, Color body, Color core, float size, int life)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count >= 1300 || !Finite(position)) return;
        new T_Flame_Particle(position, velocity, body, core, size, life).Spawn();
    }
}

public sealed class OperatorFlightVfxSystem : ModSystem
{
    public override void Unload() => OperatorFlightVfx.Unload();
}
