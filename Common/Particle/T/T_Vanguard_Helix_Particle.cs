using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// A short-lived mote orbiting its actual release point, then fading into the wake.
// Its projected depth controls size/brightness; paired motes use phases separated by PI.
public sealed class T_Vanguard_Helix_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly Vector2 origin, axis;
    private readonly float phase, radius;
    private Vector2 previous;
    public T_Vanguard_Helix_Particle(Vector2 point, Vector2 direction, VanguardSpearKind kind, float phase)
    {
        origin = point; axis = direction.SafeNormalize(Vector2.UnitX); this.phase = phase;
        radius = 20 + (int)kind * 3; Lifetime = 18;
        Color = VanguardSpearRules.Tint(kind); Position = previous = PointAt(0);
    }
    private Vector2 PointAt(float time) => origin - axis * (time * 1.1f)
        + axis.RotatedBy(MathHelper.PiOver2) * MathF.Sin(phase + time * .36f) * (radius + time * .25f);
    public override void Update() { previous = PointAt(Math.Max(0, Time - 1)); Position = PointAt(Time); Velocity = Vector2.Zero; }
    public override void Draw()
    {
        float depth = .55f + .45f * (.5f + .5f * MathF.Cos(phase + Time * .36f));
        float fade = (1 - LifetimeRatio) * depth;
        Vector2 delta = Position - previous;
        float length = Math.Clamp(delta.Length(), 2, 18);
        Main.spriteBatch.Draw(Texture, previous - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color * fade,
            delta.SafeNormalize(axis).ToRotation(), new Vector2(0, .5f), new Vector2(length, 1.4f * depth), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color * (fade * .85f),
            MathHelper.PiOver4, new Vector2(.5f), new Vector2(2.2f * depth), SpriteEffects.None, 0);
    }
}
