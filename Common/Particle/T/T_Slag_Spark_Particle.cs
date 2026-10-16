using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Laevatain;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Slag_Spark_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Content/Projectiles/Guard/Mousse/Assets/MousseLightSplinter";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly float length;

    public T_Slag_Spark_Particle(Vector2 position, Vector2 velocity, Color color, float length, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        this.length = length;
        Lifetime = lifetime;
    }

    public override void Update() => Velocity *= 0.88f;

    public override void Draw()
    {
        float fade = 1f - LifetimeRatio;
        Vector2 scale = new Vector2(length, length * 0.4f) / Texture.Size();
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade,
            Velocity.LengthSquared() > 0.01f ? Velocity.ToRotation() : 0f,
            Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }
}
