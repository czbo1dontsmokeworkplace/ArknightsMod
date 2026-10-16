using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

internal static class ChargedOperatorVfx
{
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);
    private static readonly Dictionary<string, Texture2D> masks = new();
    private static ParticlePool<PrettySparkleParticle> sparkles;
    private static MiscShaderData ribbon;
    private const string Assets = "ArknightsMod/Content/Textures/OperatorVfx/";
    internal static bool Visible(Vector2 center) => !Main.dedServ && Vector2.DistanceSquared(center,
        Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2) < 1800 * 1800;
    internal static void Unload()
    {
        Texture2D[] old = new Texture2D[masks.Count]; masks.Values.CopyTo(old, 0); masks.Clear();
        Main.QueueMainThreadAction(() => { foreach (var texture in old) texture.Dispose(); });
        sparkles = null; ribbon = null;
    }
    // These six source PNGs were checked to contain RGB > Alpha in translucent pixels.
    // Check the LOADED pixels too: loaders that premultiply remove that signature. Normalize
    // a private copy to straight RGBA, retaining grayscale detail as well as the silhouette.
    // Never modify the shared asset, and never simulate light with an alpha-zero draw tint.
    private static Texture2D Mask(string name)
    {
        if (masks.TryGetValue(name, out Texture2D mask)) return mask;
        Texture2D source = ModContent.Request<Texture2D>(Assets + name).Value;
        Color[] pixels = new Color[source.Width * source.Height]; source.GetData(pixels);
        bool straight = false;
        foreach (Color pixel in pixels)
            if (pixel.A > 0 && pixel.A < 255 && Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > pixel.A + 4)
            { straight = true; break; }
        if (!straight)
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                pixels[i] = pixel.A == 0 ? Color.Transparent : new Color(
                    Math.Min(255, pixel.R * 255 / pixel.A), Math.Min(255, pixel.G * 255 / pixel.A),
                    Math.Min(255, pixel.B * 255 / pixel.A), pixel.A);
            }
        mask = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
        mask.SetData(pixels); masks.Add(name, mask); return mask;
    }
    internal readonly struct Pass : IDisposable
    {
        public Pass(bool additive)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, additive ? BlendState.Additive : BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public void Dispose()
        {
            Main.spriteBatch.End(); Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void Sprite(string name, Vector2 center, Vector2 size, Color color, float rotation = 0)
    {
        if (!Visible(center) || !float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0 || size.X > 640 || size.Y > 640) return;
        Texture2D texture = Mask(name);
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, rotation, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }
    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() > 3300 * 3300 || delta.LengthSquared() < .01f) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, Pixel, color, delta.ToRotation(),
            new Vector2(0, .5f), new Vector2(delta.Length(), Math.Clamp(width, .5f, 40f)), SpriteEffects.None);
    }
    internal static void Sparkle(Vector2 position, Vector2 velocity, Color color, float angle, float scale = 1f)
    {
        if (!Visible(position)) return;
        sparkles ??= new(256, () => new PrettySparkleParticle());
        PrettySparkleParticle p = sparkles.RequestParticle();
        p.LocalPosition = position; p.Velocity = velocity; p.ColorTint = color; p.Rotation = angle;
        p.Scale = new Vector2(1.3f, .35f) * scale; p.TimeToLive = 18;
        p.FadeInNormalizedTime = .02f; p.FadeOutNormalizedTime = .9f;
        p.FadeInEnd = 3; p.FadeOutStart = 7; p.FadeOutEnd = 18; p.AdditiveAmount = .65f;
        Main.ParticleSystem_World_OverPlayers.Add(p);
    }
    internal static void Burst(Vector2 center, ChargedOperatorKind kind, int count, float radius)
    {
        if (!Visible(center)) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        count = Math.Clamp(count, 1, 24); radius = Math.Clamp(radius, 8, 120);
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            Vector2 radial = angle.ToRotationVector2();
            if (ParticleManager.activeParticles.Count < 1200)
                new T_Operator_Fragment_Particle(center + radial * radius * .15f,
                    radial * Main.rand.NextFloat(1.6f, 4.2f), color, kind, 18 + i % 12, 5 + i % 5).Spawn();
            if (i % 3 == 0) Sparkle(center + radial * radius * .55f, radial * .45f, color, angle);
            if (i % 6 == 0)
            {
                Dust dust = Dust.NewDustPerfect(center, Terraria.ID.DustID.TintableDustLighted, radial * 2f, 90, color, .7f);
                dust.noGravity = true;
            }
        }
    }
    internal static void Trail(Projectile p, Color color, float width)
    {
        if (!Visible(p.Center)) return;
        List<Vector2> positions = new() { p.Center }; float length = 0;
        foreach (Vector2 old in p.oldPos)
        {
            if (old == Vector2.Zero) break;
            Vector2 next = old + p.Size / 2; float distance = Vector2.Distance(positions[^1], next);
            if (!float.IsFinite(distance) || distance > 96 || length + distance > 280) break;
            if (distance < .5f) continue;
            positions.Add(next); length += distance;
        }
        if (positions.Count < 3) return;
        Vector2[] points = positions.ToArray(); float[] rotations = new float[points.Length];
        for (int i = 0; i < points.Length; i++) rotations[i] = (points[Math.Max(0, i - 1)] - points[Math.Min(points.Length - 1, i + 1)]).ToRotation();
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
#pragma warning disable CS0618
            ribbon ??= new MiscShaderData(Main.VertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(true)
                .UseImage0("Images/Extra_191").UseImage1("Images/Extra_189").UseImage2("Images/Extra_190");
#pragma warning restore CS0618
            ribbon.UseColor(color).UseSecondaryColor(Color.Lerp(color, Color.White, .25f)).UseOpacity(.7f).UseSaturation(0).Apply();
            VertexStrip strip = new();
            strip.PrepareStrip(points, rotations, t => color * (1 - t), t => Math.Clamp(width, 2, 24) * (1 - t), -Main.screenPosition);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply(); Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
public sealed class ChargedOperatorVfxSystem : ModSystem
{
    public override void Unload() => ChargedOperatorVfx.Unload();
}
