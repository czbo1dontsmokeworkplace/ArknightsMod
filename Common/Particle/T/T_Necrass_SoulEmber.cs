using System;
using ArknightsMod.Content.Projectiles.Caster.Necrass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Material-backed fragments belonging only to Necrass; no shared particle style changes.
public sealed class T_Necrass_SoulEmber : Particle
{
    public override string TexturePath => NecrassArt.Assets + "SoulHead";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly float size, phase, curl;
    private readonly int kind;

    public T_Necrass_SoulEmber(Vector2 point, Vector2 drift, Color color, int life, float size, int kind)
    {
        Position = point; Velocity = drift; Color = color;
        Lifetime = Math.Clamp(life, 8, 50); this.size = size; this.kind = kind;
        phase = Main.rand.NextFloat(4); curl = Main.rand.NextFloat(-.025f, .025f);
    }

    public override void Update()
    {
        Velocity = (Velocity * .96f).RotatedBy(curl);
        if (kind != 0) Velocity.Y -= .005f;
    }

    public override void Draw()
    {
        if (NecrassVisuals.Ready())
            NecrassArt.Ember(Main.spriteBatch, Position - Main.screenPosition, Velocity,
                size, LifetimeRatio, phase, Color, kind, true);
    }
}
