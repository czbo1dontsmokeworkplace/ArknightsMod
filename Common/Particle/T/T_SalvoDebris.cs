using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_SalvoDebris : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float size;
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_SalvoDebris(Vector2 position, Vector2 velocity, float size)
    {
        Position = position; Velocity = velocity; this.size = size;
        Color = new Color(255, 100, 30); Lifetime = 32 + (int)(size * 3);
        Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= .965f; Velocity.Y += .12f;
        Rotation = Velocity.ToRotation();
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.8f);
        Vector2 at = Position - Main.screenPosition;
        Rectangle pixel = new(0, 0, 1, 1);
        Main.spriteBatch.Draw(Texture, at, pixel, Color * fade, Rotation,
            new Vector2(1, .5f), new Vector2(size + Velocity.Length() * 1.8f, 2.4f * fade), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, at, pixel, new Color(255, 235, 180) * fade, Rotation,
            new Vector2(1, .5f), new Vector2(size * .7f, 1.1f * fade), SpriteEffects.None, 0);
    }
}
