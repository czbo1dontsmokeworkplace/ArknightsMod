using System;
using ArknightsMod.Common.Particle;
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
    internal static MostimaVisuals Begin()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
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
        Texture2D texture = ModContent.Request<Texture2D>(Assets + name).Value;
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), flip, 0);
    }
    internal static void Dial(Vector2 center, Vector2 size, float angle, Color tint)
    {
        // The source is a right semicircle with a deliberate canvas-center pivot.
        Sprite("ChronoDial", center, size, tint, angle);
        Sprite("ChronoDial", center, size, tint, angle, SpriteEffects.FlipHorizontally);
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
    internal static void Charge(Vector2 center, Vector2 target, float charge, float time)
    {
        using var pass = Begin();
        float size = 35 + 75 * charge;
        // Opposite rotating elliptical orbits, with separate small cores along their paths.
        for (int ring = 0; ring < 2; ring++)
        {
            float angle = (ring == 0 ? .55f : -.55f) + time * (ring == 0 ? .3f : -.25f);
            Dial(center, new Vector2(size, size * .38f), angle, (ring == 0 ? Blue : Violet) * (.22f + charge * .4f));
            for (int i = 0; i < 5; i++)
            {
                float phase = time * (ring == 0 ? 1.8f : -1.8f) + i * MathHelper.TwoPi / 5;
                Vector2 offset = new Vector2(MathF.Cos(phase) * size * .44f, MathF.Sin(phase) * size * .17f).RotatedBy(angle);
                Sprite("ChronoCore", center + offset, new Vector2(7 + 5 * charge), Blue * (.3f + .45f * charge), phase);
            }
        }
        Cross(center, 30 + charge * 28, .55f + .3f * charge, time * .6f);
        Dial(target, new Vector2(160 + 320 * charge), -time * .13f, Blue * (.14f + charge * .18f));
        Cross(target, 160 + 320 * charge, .13f + .18f * charge);
        Sprite("ChronoHand", target, new Vector2(140, 75), Violet * .45f, -time * .7f);
    }
    internal static void Burst(Vector2 center, float age)
    {
        using var pass = Begin();
        float progress = MathHelper.Clamp(age / 44f, 0, 1);
        float fade = MathF.Pow(1 - progress, 1.15f);
        float expansion = 1 - MathF.Pow(1 - MathHelper.Clamp(age / 13f, 0, 1), 3);
        float diameter = MathHelper.Lerp(120, 560, expansion);
        Dial(center, new Vector2(diameter), .12f * progress, Blue * fade * .85f);
        Dial(center, new Vector2(diameter * .73f), -.3f * progress, Violet * fade * .48f);
        Cross(center, diameter, fade * .95f);
        // A delayed, wider and dimmer pulse gives the impact a readable core and pressure front.
        if (age > 6)
            Dial(center, new Vector2(350 + progress * 410), -.08f, Blue * fade * .24f);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * MathHelper.TwoPi / 5 + .3f;
            Sprite("ChronoCore", center + angle.ToRotationVector2() * (45 + 150 * expansion),
                new Vector2(16 + 20 * fade), Violet * fade * .65f, angle);
        }
    }
    internal static void Particles(Vector2 center, int count, float speed)
    {
        if (Main.dedServ || ParticleManager.activeParticles.Count > 1300) return;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.1f, .1f);
            new MostimaChronoParticle(center, angle.ToRotationVector2() * speed * Main.rand.NextFloat(.65f, 1.2f),
                i % 2 == 0 ? Blue : Violet, Main.rand.Next(22, 38)).Spawn();
        }
    }
}

public sealed class MostimaChronoParticle : Particle
{
    private readonly Color tint;
    public override string TexturePath => MostimaVisuals.Assets + "ChronoStreak";
    public override BlendState DrawBlendState => BlendState.Additive;
    public MostimaChronoParticle(Vector2 center, Vector2 velocity, Color color, int lifetime)
    {
        Position = center; Velocity = velocity; tint = Color = color; Lifetime = lifetime;
        Direction = 1;
    }
    public override void Update()
    {
        Velocity *= .945f;
        Rotation = Velocity.ToRotation() - .91f;
        // Bound each streak to 8..25 pixels instead of stretching a pixel across the scene.
        Scale = MathHelper.Clamp(Velocity.Length() * .035f + .12f, .12f, .39f);
        Color = tint * MathF.Pow(1 - LifetimeRatio, 1.3f);
    }
}
