using ArknightsMod.Common.Particle.T;
using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

// Neutral straight-RGBA masks. Ordinary art uses NonPremultiplied; luminous masks use real Additive.
internal sealed class MechAccordMaterials : ModSystem
{
    private static readonly Dictionary<string, Texture2D> masks = new();
    internal static Texture2D Mask(string name)
    {
        if (masks.TryGetValue(name, out Texture2D result)) return result;
        const int size = 128;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = (x + .5f - size * .5f) / (size * .5f);
            float dy = (y + .5f - size * .5f) / (size * .5f);
            float r = MathF.Sqrt(dx * dx + dy * dy);
            float alpha = name switch
            {
                "Ring" => MathF.Exp(-MathF.Pow((r - .79f) / .045f, 2f)),
                "Iris" => MathF.Exp(-MathF.Pow((r - .66f) / .12f, 2f)) *
                    (.65f + .35f * MathF.Cos(MathF.Atan2(dy, dx) * 8f)),
                "Arrow" => MathF.Pow(Math.Clamp(1f - MathF.Abs(dx) * 1.4f - MathF.Abs(dy), 0f, 1f), .7f),
                _ => MathF.Exp(-r * r * 7f) * Math.Clamp((1f - r) * 6f, 0f, 1f)
            };
            pixels[y * size + x] = new Color(255, 255, 255, (byte)(255f * Math.Clamp(alpha, 0f, 1f)));
        }
        result = new Texture2D(Main.spriteBatch.GraphicsDevice, size, size);
        result.SetData(pixels); masks[name] = result; return result;
    }
    public override void Unload()
    {
        Texture2D[] owned = new Texture2D[masks.Count]; masks.Values.CopyTo(owned, 0);
        masks.Clear();
        MechAccordVfx.Unload();
        if (!Main.dedServ) Main.QueueMainThreadAction(() => { foreach (Texture2D t in owned) t.Dispose(); });
    }
}

internal static class MechAccordVfx
{
    private static MiscShaderData ribbonShader;
    internal static void Unload() => ribbonShader = null;
    private static MiscShaderData RibbonShader
    {
        get
        {
            if (ribbonShader != null) return ribbonShader;
            // Independent parameter state; changing Rockrock must not recolor another weapon's trail.
#pragma warning disable CS0618
            ribbonShader = new MiscShaderData(Main.VertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(true);
#pragma warning restore CS0618
            return ribbonShader.UseImage0(Terraria.GameContent.TextureAssets.Extra[191])
                .UseImage1(Terraria.GameContent.TextureAssets.Extra[189])
                .UseImage2(Terraria.GameContent.TextureAssets.Extra[190]);
        }
    }
    internal static readonly Color Violet = new(125, 82, 242);
    internal static readonly Color Amber = new(255, 168, 71);
    internal static Color Fade(Color color, float opacity) => new(color.R, color.G, color.B,
        (byte)(color.A * Math.Clamp(opacity, 0f, 1f)));
    internal static bool Visible(Vector2 point) => !Main.dedServ && point.X > Main.screenPosition.X - 200 &&
        point.X < Main.screenPosition.X + Main.screenWidth + 200 && point.Y > Main.screenPosition.Y - 200 &&
        point.Y < Main.screenPosition.Y + Main.screenHeight + 200;

    internal sealed class Pass : IDisposable
    {
        internal Pass(BlendState blend)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public void Dispose()
        {
            Main.spriteBatch.End();
            Main.pixelShader?.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void Mask(string name, Vector2 center, Vector2 size, Color tint, float angle = 0f)
    {
        Texture2D texture = MechAccordMaterials.Mask(name);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, angle,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
    }

    internal static void DrawLens(Vector2 center, bool rockrock, float diameter, float age)
    {
        using var pass = new Pass(BlendState.Additive);
        Mask("Bloom", center, new Vector2(diameter), (rockrock ? Violet : Color.White) * .45f);
        Mask(rockrock ? "Iris" : "Ring", center, new Vector2(diameter),
            (rockrock ? Amber : new Color(205, 220, 235)) * .7f, age * .012f);
        Mask("Bloom", center, new Vector2(diameter * .4f), Color.White * .82f);
    }

    internal static void DrawBurst(Vector2 center, float progress, bool rockrock, float diameter)
    {
        using var light = new Pass(BlendState.Additive);
        float fade = MathF.Pow(1f - progress, 1.5f);
        Mask("Bloom", center, new Vector2(diameter * .9f), Fade(rockrock ? Violet : Color.White, fade * .5f));
        Mask("Ring", center, new Vector2(diameter * (.35f + progress * .8f)),
            Fade(rockrock ? Amber : Color.White, fade * .75f));
        Mask("Iris", center, new Vector2(diameter * (.45f + progress * .35f)),
            Fade(rockrock ? Violet : Color.White, fade * .42f), progress * (rockrock ? 1.2f : .15f));
        Mask("Bloom", center, new Vector2(diameter * .32f), Fade(Color.White, MathF.Pow(1f - progress, 3f) * .95f));
    }

    internal static void DrawStar(Vector2 center, float angle, float fade, float visualScale = 1)
    {
        using var light = new Pass(BlendState.Additive);
        Mask("Bloom", center, new Vector2(42f, 27f) * visualScale, Fade(Violet, .65f * fade), angle);
        Mask("Arrow", center, new Vector2(15f, 34f) * visualScale, Fade(Amber, .95f * fade), angle + MathHelper.PiOver2);
        Mask("Arrow", center, new Vector2(5f, 24f) * visualScale, Fade(Color.White, fade), angle + MathHelper.PiOver2);
    }

    // Samples are real flight-history points; the rotations below are path tangents, not sprite angles.
    internal static void DrawTrail(Vector2[] points, float opacity, float visualScale = 1)
    {
        if (points.Length < 3) return;
        float[] tangents = new float[points.Length];
        for (int i = 0; i < points.Length; i++)
            tangents[i] = (points[Math.Max(0, i - 1)] - points[Math.Min(points.Length - 1, i + 1)]).ToRotation();
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            MiscShaderData shader = RibbonShader;
            shader.UseColor(Violet); shader.UseSecondaryColor(Amber);
            shader.UseOpacity(4f * opacity); shader.UseSaturation(-2f); shader.Apply();
            VertexStrip strip = new();
            strip.PrepareStrip(points, tangents,
                t => Color.Lerp(Amber, Violet, Math.Clamp(t * 1.5f, 0f, 1f)) * (opacity * MathF.Pow(1f - t, 1.2f)),
                t => 9f * visualScale * MathF.Pow(1f - t, .8f), -Main.screenPosition);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply(); Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void DrawRail(Vector2 start, Vector2 end, float fade)
    {
        using var light = new Pass(BlendState.Additive);
        Vector2 delta = end - start;
        float length = Math.Min(delta.Length(), 440f), angle = delta.ToRotation();
        // Broad soft corona, thin white core; both terminate at the actual target, never span the screen.
        Mask("Bloom", (start + end) * .5f, new Vector2(length + 14f, 17f * fade), Violet * (.65f * fade), angle);
        Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        Rectangle source = new(0, 0, 1, 1);
        foreach (float width in new[] { 4.5f, 1.3f })
            Main.spriteBatch.Draw(pixel, start - Main.screenPosition, source,
                (width > 2f ? Amber : Color.White) * fade, angle, new Vector2(0, .5f),
                new Vector2(length, Math.Max(.2f, width * fade)), SpriteEffects.None, 0f);
        Mask("Ring", start, new Vector2(22f * fade, 13f * fade), Amber * fade, angle);
        Mask("Bloom", end, new Vector2(24f * fade), Color.White * (.75f * fade));
    }
}

internal static class MechAccordVisuals
{
    internal static void Wake(Vector2 point, Vector2 velocity, int age)
    {
        if (!MechAccordVfx.Visible(point) || ParticleManager.activeParticles.Count >= 1100) return;
        Vector2 tangent = velocity.SafeNormalize(Vector2.UnitX);
        Vector2 sideways = new(-tangent.Y, tangent.X);
        new T_Mech_Radial_Particle(point, -tangent * .7f + sideways * (age % 2 == 0 ? .65f : -.65f),
            age % 2 == 0 ? MechAccordVfx.Violet : MechAccordVfx.Amber, 3.5f, 18).Spawn();
    }
    internal static void Charge(Vector2 tip, bool rockrock)
    {
        if (!MechAccordVfx.Visible(tip) || ParticleManager.activeParticles.Count >= 1100) return;
        Vector2 offset = Main.rand.NextVector2Unit() * 14f;
        new T_Mech_Radial_Particle(tip + offset, -offset * .055f,
            rockrock ? MechAccordVfx.Amber : Color.White, 3.5f, 14).Spawn();
    }
    internal static void Overload(Vector2 center) => Emit(center, true, false);
    internal static void Emit(Vector2 center, bool rockrock, bool impact = true)
    {
        if (!MechAccordVfx.Visible(center)) return;
        int count = impact ? (rockrock ? 12 : 9) : 3;
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 1200; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / count + Main.rand.NextFloat(-.09f, .09f)).ToRotationVector2();
            new T_Mech_Radial_Particle(center + direction * 4f, direction * Main.rand.NextFloat(.8f, 2.6f),
                rockrock ? (i % 2 == 0 ? MechAccordVfx.Violet : MechAccordVfx.Amber) : Color.White,
                Main.rand.NextFloat(2.5f, 5.5f), Main.rand.Next(13, 24)).Spawn();
        }
        if (impact)
        {
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.SilverCoin,
                    Main.rand.NextVector2Circular(1.4f, 1.4f), 120, Color.White, .55f); d.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .16f, Pitch = rockrock ? -.1f : .55f,
                MaxInstances = 4 }, center);
        }
    }
}
