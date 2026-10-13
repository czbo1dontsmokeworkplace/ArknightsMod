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
        // The target preview and the released clock use exactly the same 560-pixel face.
        float diameter = MathHelper.Lerp(160f, 560f, charge);
        Dial(target, new Vector2(diameter), 0f, Blue * (.10f + charge * .30f));
        Dial(target, new Vector2(diameter * .67f), -time * .25f, Violet * (.06f + charge * .16f));
        Sprite("TemporalSmear", target, new Vector2(diameter * .84f), Blue * (.10f * charge), time * .15f);
        DrawClockMarks(target, diameter * .405f, charge * .32f, 0f);
    }
    internal static void Burst(Vector2 center, float age)
    {
        float appear = EaseOut(MathHelper.Clamp(age / 7f, 0f, 1f));
        float dissolve = MathHelper.Clamp((65f - age) / 12f, 0f, 1f);
        float face = appear * dissolve;
        float impact = MathF.Max(Pulse(age, 10f, 8f), age >= 10f ? .34f * Pulse(age % 10f, 0f, 3f) : 0f);
        const float diameter = 560f;

        // A local pixel shader curls the grayscale vortex texture toward a dark center.
        // It changes this texture's UVs only, leaving the rest of the game world alone.
        DrawWarpedSmear(center, diameter * 1.02f, age, face);

        // Dark painted cavity comes first. The source image is straight RGBA, so use
        // NonPremultiplied and retain a nonzero vertex alpha; never rely on alpha-zero black.
        using (Begin(BlendState.NonPremultiplied))
        {
            Sprite("TemporalSmear", center, new Vector2(diameter * .9f),
                new Color(7, 22, 49) * (.20f * face), -age * .055f);
            Sprite("TemporalOrb", center, new Vector2(diameter * .41f),
                new Color(6, 14, 34) * (.48f * face));
        }

        // Two short spiral ribbons give the clock a distorted time-flow contour.
        // The shader and trail texture are local to this mod; neither points to a reference mod.
        if (age is >= 5f and <= 36f)
            DrawChronoRibbons(center, age, diameter * .39f, face * .34f);

        using var pass = Begin();
        Sprite("TemporalSmear", center, new Vector2(diameter * .97f),
            Blue * (.12f * face), age * .024f);
        Sprite("TemporalSmear", center, new Vector2(diameter * .67f),
            Violet * (.11f * face), -age * .034f);

        // The supplied half dial establishes the outline. Minute and hour marks are assembled
        // independently below, like the moving particle clock in StarsofDestinyLEFTCLK.
        Dial(center, new Vector2(diameter), 0f, Blue * (.62f * face));
        Dial(center, new Vector2(diameter * .75f), 0f, Violet * (.24f * face));
        DrawClockMarks(center, diameter * .405f, face, 0f);

        float turn = MathHelper.Clamp(age / 65f, 0f, 1f);
        DrawClockHand(center, diameter * .24f, -MathHelper.PiOver2 + MathHelper.Pi * turn,
            face, Blue);
        DrawClockHand(center, diameter * .36f, -MathHelper.PiOver2 + MathHelper.TwoPi * 8f * turn,
            face, new Color(220, 241, 255));

        // A cross remains visible behind the hands, while distinct pulses supply the explosion.
        Sprite("TemporalFlare", center, new Vector2(120f + impact * 260f),
            new Color(142, 205, 255) * (impact * .30f));
        Sprite("TemporalOrb", center, new Vector2(42f + impact * 45f),
            new Color(225, 242, 255) * (.42f * impact + .18f * face));

        for (int i = 0; i < 8; i++)
        {
            float angle = MathHelper.TwoPi * i / 8f + .13f;
            Vector2 direction = angle.ToRotationVector2();
            Vector2 point = center + direction * diameter * .43f;
            Sprite("TemporalStar", point, new Vector2(20f + impact * 16f),
                (i % 2 == 0 ? Blue : Violet) * (.19f * face + .28f * impact), angle);
        }
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

    private static void DrawClockMarks(Vector2 center, float radius, float opacity, float age)
    {
        // Fixed minute and hour indices keep the clock face aligned with the world.
        for (int i = 0; i < 60; i++)
        {
            float angle = MathHelper.TwoPi * i / 60f - MathHelper.PiOver2;
            Vector2 direction = angle.ToRotationVector2();
            bool hour = i % 5 == 0;
            float length = hour ? 22f : 8f;
            for (int section = 0; section < (hour ? 3 : 1); section++)
            {
                float distance = radius - length + (section + .5f) * length / (hour ? 3 : 1);
                Sprite("TemporalOrb", center + direction * distance,
                    new Vector2(hour ? 10f : 5f),
                    (hour ? new Color(224, 240, 255) : Blue) * (opacity * (hour ? .60f : .35f)));
            }
        }
    }

    private static void DrawClockHand(Vector2 center, float length, float angle, float opacity, Color color)
    {
        Vector2 direction = angle.ToRotationVector2();
        Sprite("ChronoHand", center + direction * length * .47f,
            new Vector2(length * 1.12f, 21f), color * (.62f * opacity), angle);
        // Short, textured beads follow each hand. They make the hand readable without a
        // screen-length pixel line and leave a bright, separately controlled arrow tip.
        for (int i = 1; i <= 10; i++)
        {
            float t = i / 10f;
            Sprite("TemporalOrb", center + direction * length * t,
                new Vector2(MathHelper.Lerp(6f, 3f, t)), color * (opacity * (.42f + .25f * t)));
        }
        Sprite("TemporalStar", center + direction * length, new Vector2(20f),
            color * (.72f * opacity), angle);
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

// Moving points on the two clock tracks, independent from the static dial texture.
public sealed class MostimaClockMote : Particle
{
    private readonly Vector2 center;
    private readonly Color tint;
    private readonly float radius;
    private readonly int spin;
    private float angle;
    public override string TexturePath => MostimaVisuals.Assets + "TemporalOrb";
    public override BlendState DrawBlendState => BlendState.Additive;
    public MostimaClockMote(Vector2 center, float radius, float angle, int spin, Color tint)
    {
        Position = this.center = center;
        this.radius = radius;
        this.angle = angle;
        this.spin = spin;
        this.tint = Color = tint;
        Lifetime = 42;
        Direction = 1;
    }
    public override void Update()
    {
        angle += .052f * spin;
        float swell = .88f + .12f * MathF.Sin(Time * .28f + angle);
        Position = center + angle.ToRotationVector2() * radius * swell;
        Color = tint * MathF.Min(1f, (Lifetime - Time) / 12f);
    }
    public override void Draw()
    {
        float diameter = spin > 0 ? 8f : 6f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, 0f,
            Texture.Size() * .5f, new Vector2(diameter) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
