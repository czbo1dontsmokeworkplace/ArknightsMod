using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_SoftSmoke_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float diameter;
    private readonly Color tint;
    public override string TexturePath => OperatorTextureEffects.SmokePath;
    public T_SoftSmoke_Particle(Vector2 position, Vector2 velocity, Color color, float size, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        tint = color;
        diameter = MathHelper.Clamp(size, 6f, 56f);
        Lifetime = Math.Clamp(lifetime, 12, 45);
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Velocity *= .92f;
        Velocity.Y -= .012f;
        Rotation += .018f;
    }
    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathHelper.Clamp(t * 8f, 0f, 1f) * (1f - t) * .65f;
        OperatorTextureEffects.Cloud(Position, diameter * (1f + t * .8f), tint, fade, Rotation);
    }
}
