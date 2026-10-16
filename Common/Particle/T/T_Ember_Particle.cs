using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Sniper.Meteorite;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Ember_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color initialColor;
    private readonly float initialSize;
    public override string TexturePath => "ArknightsMod/Content/Textures/muzzle_02";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Ember_Particle(Vector2 position, Vector2 velocity, int lifetime, Color color, float size)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        initialColor = Color = color;
        initialSize = size;
        Rotation = velocity.ToRotation() - MathHelper.PiOver2;
    }

    public override void Update()
    {
        Velocity *= .90f;
        Rotation += .035f;
        Color = initialColor * MathF.Pow(1f - LifetimeRatio, 1.6f);
    }

    public override void Draw()
    {
        float size = initialSize * (1f - LifetimeRatio * .65f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(size / Texture.Width, size * 1.4f / Texture.Height),
            SpriteEffects.None, 0f);
    }
}
