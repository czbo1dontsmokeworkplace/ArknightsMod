using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Laevatain;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Slag_Drop_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Content/ElementalImpairment/Effect/FireParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Slag_Drop_Particle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        Scale = scale;
        Color = color;
    }

    public override void Update()
    {
        Velocity.X *= 0.97f;
        Velocity.Y += 0.25f;
        Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
    }

    public override void Draw()
    {
        float fade = MathF.Pow(1f - LifetimeRatio, 1.3f);
        Vector2 size = new Vector2(8f, 12f) * Scale * fade;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * fade,
            Rotation, Texture.Size() * 0.5f, size / Texture.Size(), SpriteEffects.None, 0f);
    }
}
