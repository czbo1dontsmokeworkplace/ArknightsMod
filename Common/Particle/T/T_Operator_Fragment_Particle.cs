using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// The mod's own particle system, distinct from vanilla Dust and pooled PrettySparkleParticle.
public sealed class T_Operator_Fragment_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float size;
    private readonly ChargedOperatorKind kind;
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Operator_Fragment_Particle(Vector2 position, Vector2 velocity, Color color, ChargedOperatorKind kind, int lifetime, float size)
    {
        Position = position; Velocity = velocity; Color = color; this.kind = kind;
        Lifetime = Math.Clamp(lifetime, 8, 36); this.size = Math.Clamp(size, 2, 12);
        Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= .94f;
        if (kind == ChargedOperatorKind.Matoimaru) Velocity.Y -= .05f;
        else if (kind == ChargedOperatorKind.Franka) Velocity.Y += .08f;
        Rotation += kind == ChargedOperatorKind.Melantha ? .09f : .025f;
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.4f);
        Vector2 dimensions = kind switch {
            ChargedOperatorKind.Melantha => new Vector2(size, size * .6f),
            ChargedOperatorKind.Matoimaru => new Vector2(size * .6f, size),
            ChargedOperatorKind.Franka => new Vector2(size * 1.8f, 2),
            _ => new Vector2(size, 2.5f)
        };
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color * fade,
            Rotation, new Vector2(.5f), dimensions, SpriteEffects.None, 0);
    }
}
