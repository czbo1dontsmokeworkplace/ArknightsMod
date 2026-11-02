using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_BossIceShard : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly float size;
    private readonly bool star;
    public T_BossIceShard(Vector2 at, Vector2 velocity, float size, bool star)
    {
        Position = at; Velocity = velocity; this.size = size; this.star = star;
        Color = new Color(135, 215, 255); Lifetime = star ? 34 : 42; Rotation = velocity.ToRotation();
    }
    public override void Update() { Velocity *= .94f; Velocity.Y += .04f; Rotation += star ? .025f : .08f; }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.5f);
        int arms = star ? 6 : 2;
        for (int i = 0; i < arms; i++)
        {
            float angle = Rotation + i * MathHelper.TwoPi / arms;
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color * fade,
                angle, new Vector2(0, .5f), new Vector2(size * (star ? 1 : 1.8f) * (.5f + fade), star ? 1.2f : 2.2f), SpriteEffects.None, 0);
            if (star) Main.spriteBatch.Draw(Texture, Position + angle.ToRotationVector2() * size * .5f - Main.screenPosition,
                new Rectangle(0, 0, 1, 1), Color.White * fade, angle + .7f, new Vector2(0, .5f), new Vector2(size * .35f, 1), SpriteEffects.None, 0);
        }
    }
}
