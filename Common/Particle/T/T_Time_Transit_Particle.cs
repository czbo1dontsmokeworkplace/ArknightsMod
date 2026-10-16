using System;
using System.Collections.Generic;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Time_Transit_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center, axis, side;
    private readonly float gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Time_Transit_Particle(Vector2 center, float angle, float gold = 0f)
    {
        this.center = center; this.gold = gold; axis = angle.ToRotationVector2(); side = axis.RotatedBy(MathHelper.PiOver2);
        Lifetime = 30; Position = Point(0f); Rotation = angle;
    }
    private Vector2 Point(float t)
    {
        float distance = MathHelper.Lerp(-660f, 660f, t * t * (3f - 2f * t));
        return center + axis * distance + side * MathF.Sin(t * MathHelper.Pi) * 50f;
    }
    public override void Update()
    {
        Position = Point(LifetimeRatio);
        Color = MostimaClockwork.TimeColor(Color.Lerp(MostimaVisuals.Violet, MostimaClockwork.Silver, LifetimeRatio), gold)
            * (MathF.Sin(MathHelper.Pi * LifetimeRatio) * .65f);
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 100f)) return;
        Vector2 previous = Point(Math.Max(0f, LifetimeRatio - .018f));
        Vector2 trail = Position - previous;
        float length = MathHelper.Clamp(trail.Length() * 2.5f, 6f, 85f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, trail.ToRotation(),
            Texture.Size() * .5f, new Vector2(length, 3f + 7f * LifetimeRatio) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
