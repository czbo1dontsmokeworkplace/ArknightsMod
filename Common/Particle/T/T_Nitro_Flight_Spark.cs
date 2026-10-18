using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Nitro's fading textured spark, colored by the firing operator. No fixed gold palette.
public sealed class T_Nitro_Flight_Spark : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/ChargedSniper/NitroSpark";
    public T_Nitro_Flight_Spark(Vector2 center, Vector2 velocity, Color tint, float size = 1, float? flightRotation = null)
    {
        Position = center; Velocity = velocity; Lifetime = 15; Color = tint; Scale = size;
        Rotation = (flightRotation ?? velocity.ToRotation()) + MathHelper.PiOver2;
    }
    public override void Update()
    {
        Scale *= .95f; Velocity *= .95f;
    }
    public override void Draw()
    {
        Color tint = Color * (1 - MathF.Pow(LifetimeRatio, 3));
        Vector2 size = new Vector2(.5f, 1.6f) * Scale;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint, Rotation, Texture.Size() / 2, size, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint, Rotation, Texture.Size() / 2, size * new Vector2(.45f, 1), SpriteEffects.None, 0);
    }
}
