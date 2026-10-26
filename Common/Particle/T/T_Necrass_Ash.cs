using System;
using ArknightsMod.Content.Projectiles.Caster.Necrass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Necrass_Ash : Particle
{
    public override string TexturePath => NecrassArt.Assets + "AshVeil";
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly float size, turn;

    public T_Necrass_Ash(Vector2 point, Vector2 drift, int life, float size)
    {
        Position = point; Velocity = drift; Lifetime = Math.Clamp(life, 8, 45); this.size = size;
        Frame = Main.rand.Next(4); Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        turn = Main.rand.NextFloat(-.014f, .014f);
    }

    public override void Update()
    {
        Velocity *= .96f; Velocity.Y -= .006f; Rotation += turn;
    }

    public override void Draw()
    {
        if (NecrassVisuals.Ready())
            NecrassArt.Smoke(Main.spriteBatch, Position - Main.screenPosition, size,
                LifetimeRatio, Frame, Rotation);
    }
}
