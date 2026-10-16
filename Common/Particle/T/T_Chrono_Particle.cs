using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Chrono_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Color tint;
    public override string TexturePath => MostimaVisuals.Assets + "ChronoStreak";
    public override BlendState DrawBlendState => BlendState.Additive;
    public T_Chrono_Particle(Vector2 center, Vector2 velocity, Color color, int lifetime)
    {
        Position = center; Velocity = velocity; tint = Color = color; Lifetime = lifetime;
        Direction = 1;
    }
    public override void Update()
    {
        Velocity *= .945f;
        Rotation = Velocity.ToRotation() - .91f;
        // Bound each streak to 8..25 pixels instead of stretching a pixel across the scene.
        Scale = MathHelper.Clamp(Velocity.Length() * .035f + .12f, .12f, .39f);
        Color = tint * MathF.Pow(1 - LifetimeRatio, 1.3f);
    }
}
