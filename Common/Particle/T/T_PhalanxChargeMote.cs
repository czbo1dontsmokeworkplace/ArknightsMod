using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Squashed light and Providence marks from the reference charge storm, recolored
// for sand/red sand/glass. Existing PhalanxSignatureVisuals remain underneath.
public class T_PhalanxChargeMote : Particle
{
    private readonly Vector2 size;
    private readonly Color tint;
    private readonly bool mark;
    public override string TexturePath => ShpcEnergyVisuals.Assets + "BloomCircle";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    internal T_PhalanxChargeMote(Vector2 position, Vector2 velocity, Color tint, int life, Vector2 size, bool mark)
    {
        Position = position; Velocity = velocity; this.tint = tint; this.size = size;
        this.mark = mark; Lifetime = life; Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= .94f;
        Rotation += mark ? .08f : 0;
    }
    public override void Draw()
    {
        float fade = Math.Min(1, Time / 3f) * MathF.Pow(1 - LifetimeRatio, 1.4f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * fade,
            Rotation, Texture.Size() * .5f, size * (1 + LifetimeRatio * .4f) / Texture.Size(), SpriteEffects.None, 0);
    }
}

public sealed class T_PhalanxChargeMark : T_PhalanxChargeMote
{
    public override string TexturePath => ShpcEnergyVisuals.Assets + "ProvidenceMarkParticle";
    internal T_PhalanxChargeMark(Vector2 position, Vector2 velocity, Color tint, int life, Vector2 size)
        : base(position, velocity, tint, life, size, true) { }
}
