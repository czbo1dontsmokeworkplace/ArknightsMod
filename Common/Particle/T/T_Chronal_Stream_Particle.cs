using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Caster.Mostima;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Local Arknights particle: curved motion blur follows the same analytic trajectory
// as the head. Emission points stay in world space as the clock tracks the mouse.
public sealed class T_Chronal_Stream_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Projectile clock;
    private readonly int identity, mode;
    private readonly Vector2 origin;
    private readonly float angle, hand, clockScale, gold;
    private float visibility;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    internal T_Chronal_Stream_Particle(Projectile clock, int mode, float angle, float hand, float scale, float gold)
    {
        this.clock = clock; identity = clock.identity; origin = clock.Center;
        this.mode = mode; this.angle = angle; this.hand = hand; clockScale = scale; this.gold = gold;
        Lifetime = mode == 2 ? 38 : 28;
        Position = Sample(0f);
    }
    private Vector2 Sample(float t) => origin + MostimaTemporalFlow.Point(mode, angle, hand, t, clockScale);
    public override void Update()
    {
        if (!clock.active || clock.identity != identity) { visibility = 0f; Kill(); return; }
        float t = LifetimeRatio;
        Position = Sample(t);
        // Smooth birth/death, and the clock's independent 15-frame release fade.
        float birth = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(t / .12f, 0f, 1f));
        float end = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((1f - t) / .22f, 0f, 1f));
        visibility = birth * end * MathHelper.Clamp(clock.ai[0] / 12f, 0f, 1f)
            * MathHelper.Clamp(1f - clock.ai[1] / MostimaClockwork.FadeTicks, 0f, 1f);
        Color = MostimaTemporalFlow.Tint(mode, gold);
    }
    public override void Draw()
    {
        if (visibility <= 0f || !SpreadVfxBudget.Visible(Position, 150f)) return;
        float t = LifetimeRatio;
        Vector2 head = Sample(t);
        const int samples = 5;
        float tailTime = mode == 2 ? .12f : .075f;
        float width = (mode == 2 ? 2.8f : 3.6f) * clockScale * (1f + gold * .25f);
        // Soft colored sleeve and a narrow luminous core, both bounded by actual
        // endpoints. A changing tangent gives the spiral a curve instead of a stick.
        for (int i = 0; i < samples; i++)
        {
            Vector2 a = Sample(Math.Max(0f, t - tailTime * i / samples));
            Vector2 b = Sample(Math.Max(0f, t - tailTime * (i + 1f) / samples));
            Vector2 d = a - b;
            float length = d.Length();
            if (length < .01f || length > 120f * clockScale) continue;
            float weight = (1f - i / (float)samples) * visibility;
            Vector2 p = (a + b) * .5f - Main.screenPosition;
            Main.spriteBatch.Draw(Texture, p, null, Color * (.38f * weight), d.ToRotation(),
                Texture.Size() * .5f, new Vector2(length + 3f, width * 3.2f) / Texture.Size(), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Texture, p, null, Color.Lerp(Color, Microsoft.Xna.Framework.Color.White, .32f) * (.75f * weight), d.ToRotation(),
                Texture.Size() * .5f, new Vector2(length + 2f, width) / Texture.Size(), SpriteEffects.None, 0f);
        }
        Main.spriteBatch.Draw(Texture, head - Main.screenPosition, null, Color * visibility,
            0f, Texture.Size() * .5f, new Vector2(width * 2f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
