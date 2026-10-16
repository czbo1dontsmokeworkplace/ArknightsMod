using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Facet_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, launch;
    private readonly Color tint;
    private readonly float size, travel, spin;
    private readonly bool stone;
    public override string TexturePath => OperatorFlightVfx.Assets + "StarCore";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    internal T_Facet_Particle(Vector2 center, Vector2 velocity, Color tint, float size, int life, float travel, bool stone)
    {
        Position = origin = center; launch = velocity; this.tint = tint;
        this.size = MathHelper.Clamp(size, 4, 30); Lifetime = Math.Clamp(life, 8, 40);
        this.travel = MathHelper.Clamp(travel, 6, 240); this.stone = stone;
        Rotation = velocity.ToRotation(); spin = Main.rand.NextFloat(-.1f, .1f);
    }
    public override void Update()
    {
        Vector2 offset = launch * ((1f - MathF.Pow(.93f, Time)) / .07f);
        if (stone) offset.Y += Time * Time * .018f;
        if (offset.LengthSquared() > travel * travel) offset = offset.SafeNormalize(Vector2.UnitY) * travel;
        Position = origin + offset; Rotation += spin;
        Color = tint * MathHelper.Clamp((Lifetime - Time) / 10f, 0f, 1f);
    }
    public override void Draw()
    {
        SpreadVfxDetail.Facet(Position, size * (stone ? .8f : .4f), size, Rotation, Color, 1f);
        if (!stone)
            SpreadVfxDetail.Segment(Position - Rotation.ToRotationVector2() * size * .26f,
                Position + Rotation.ToRotationVector2() * size * .34f,
                OperatorFlightVfx.Add(tint, .6f * (1f - LifetimeRatio)), 1f);
    }
}
