using System;
using ArknightsMod.Content.Projectiles.Caster.Passenger;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Original geometry: compact broken strokes, drawn with Terraria's white pixel.
// No Calamity textures or particle implementation are used.
public sealed class T_Chain_Bolt_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2[] points = new Vector2[6];
    private readonly float width;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Chain_Bolt_Particle(Vector2 position, Vector2 velocity, Color color, float size, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        Lifetime = lifetime;
        Rotation = velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        width = MathHelper.Clamp(size * .11f, 1.4f, 2.6f);
        float flip = Main.rand.NextBool() ? 1f : -1f;
        points[0] = new Vector2(-.5f, 0f) * size;
        points[1] = new Vector2(-.22f, -.18f * flip) * size;
        points[2] = new Vector2(-.08f, .12f * flip) * size;
        points[3] = new Vector2(.12f, -.14f * flip) * size;
        points[4] = new Vector2(.23f, .09f * flip) * size;
        points[5] = new Vector2(.5f, 0f) * size;
        for (int i = 1; i < points.Length - 1; i++)
            points[i] += new Vector2(Main.rand.NextFloat(-.07f, .07f), Main.rand.NextFloat(-.09f, .09f)) * size;
    }

    public override void Update() => Velocity *= .88f;

    public override void Draw()
    {
        float fade = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((LifetimeRatio - .3f) / .7f, 0f, 1f));
        float shrink = 1f - LifetimeRatio * .45f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = Position + (points[i] * shrink).RotatedBy(Rotation);
            Vector2 b = Position + (points[i + 1] * shrink).RotatedBy(Rotation);
            PassengerFissionArc.DrawLine(Main.spriteBatch, a, b, Color * (fade * .16f), width * 3f);
            PassengerFissionArc.DrawLine(Main.spriteBatch, a, b, Color * fade, width);
            PassengerFissionArc.DrawLine(Main.spriteBatch, a, b,
                Microsoft.Xna.Framework.Color.Lerp(Color, Microsoft.Xna.Framework.Color.White, .55f) * fade, width * .45f);
        }
    }
}

// A complete, short-lived straight discharge between two already selected enemies.
public sealed class T_Chain_Beam_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 end;
    private readonly float width;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Chain_Beam_Particle(Vector2 start, Vector2 end, Color color, float intensity)
    {
        Position = start;
        this.end = end;
        Color = color;
        width = 2f * intensity;
        Lifetime = 7;
    }

    public override void Draw()
    {
        float fade = MathF.Pow(1f - LifetimeRatio, 2f);
        PassengerFissionArc.DrawLine(Main.spriteBatch, Position, end, Color * (fade * .12f), width * 5f);
        PassengerFissionArc.DrawLine(Main.spriteBatch, Position, end, Color * (fade * .65f), width * 2f);
        PassengerFissionArc.DrawLine(Main.spriteBatch, Position, end,
            Microsoft.Xna.Framework.Color.Lerp(Color, Microsoft.Xna.Framework.Color.White, .85f) * fade, width * .65f);
    }
}
