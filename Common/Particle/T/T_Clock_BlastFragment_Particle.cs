using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Clock_BlastFragment_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, launch;
    private readonly Color tint;
    private readonly bool smoke;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => smoke ? BlendState.NonPremultiplied : BlendState.Additive;
    internal T_Clock_BlastFragment_Particle(Vector2 center, Vector2 velocity, Color tint, bool smoke)
    {
        Position = origin = center; launch = velocity; this.tint = tint; this.smoke = smoke;
        Lifetime = smoke ? 30 : 22; Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Vector2 offset = launch * ((1f - MathF.Pow(.92f, Time)) / .08f);
        if (offset.Length() > 300f) offset = offset.SafeNormalize(Vector2.UnitX) * 300f;
        Position = origin + offset;
        Color = (smoke ? new Color(24, 18, 49) : tint) * MathF.Pow(1f - LifetimeRatio, 1.2f);
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 120f)) return;
        Vector2 dimensions = smoke ? new Vector2(75f + LifetimeRatio * 65f) : new Vector2(38f * (1f - LifetimeRatio), 7f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, dimensions / Texture.Size(), SpriteEffects.None, 0f);
    }
}
