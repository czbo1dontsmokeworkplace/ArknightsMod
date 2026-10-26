using System;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_CoreCasterParticle : Particle
{
    public override string TexturePath => CoreCasterArt.Assets + "PurifiedGelBall";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly CoreCasterKind kind;
    private readonly int variant, seed;
    private readonly float size, turn;
    public T_CoreCasterParticle(Vector2 point, Vector2 drift, CoreCasterKind kind, int life, float size, int variant)
    {
        Position = point; Velocity = drift; this.kind = kind; this.size = size; this.variant = variant;
        Lifetime = Math.Clamp(life, 6, 45); seed = Main.rand.Next(64); turn = Main.rand.NextFloat(-.025f, .025f);
    }
    public override void Update()
    {
        Velocity = (Velocity * .95f).RotatedBy(turn);
        if (variant % CoreCasterArt.CrimsonPalette is 1 or 3 or 4) Velocity.Y -= .008f;
    }
    public override void Draw()
    {
        if (CoreCasterVisuals.Ready()) CoreCasterArt.Mote(Main.spriteBatch, Position - Main.screenPosition,
            Velocity, kind, size, LifetimeRatio, seed, variant);
    }
}
