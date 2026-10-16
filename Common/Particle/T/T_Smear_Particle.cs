using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

/// <summary>A drifting, fixed-size smear; emitters supply dimensions and decay.</summary>
public sealed class T_Smear_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color initialColor;
    private readonly float width, height, damping, fadeExponent;
    public override string TexturePath =>
        "ArknightsMod/Content/Projectiles/Sniper/Schwarz/Assets/SchwarzSmear";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Smear_Particle(Vector2 position, Vector2 velocity, int lifetime,
        Color color, float width, float height, float damping, float fadeExponent,
        float? rotation = null)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        initialColor = Color = color;
        this.width = width;
        this.height = height;
        this.damping = damping;
        this.fadeExponent = fadeExponent;
        Rotation = rotation ?? velocity.ToRotation();
    }

    public override void Update()
    {
        Velocity *= damping;
        Color = initialColor * MathF.Pow(1f - LifetimeRatio, fadeExponent);
    }

    public override void Draw() =>
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(width / Texture.Width, height / Texture.Height),
            SpriteEffects.None, 0f);
}
