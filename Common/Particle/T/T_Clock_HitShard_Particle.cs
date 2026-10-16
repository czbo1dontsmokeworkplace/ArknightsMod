using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Clock_HitShard_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, ray;
    private readonly float distance;
    private readonly int kind;
    private readonly float gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Clock_HitShard_Particle(Vector2 origin, float angle, float size, int kind, float gold = 0f)
    {
        Position = this.origin = origin; ray = angle.ToRotationVector2();
        this.kind = kind; this.gold = gold; Rotation = angle;
        distance = (MathHelper.Clamp(size, 16f, 64f) + 32f) * (.55f + kind * .12f);
        Lifetime = 12 + kind * 3;
    }
    private Vector2 Point(float t) => origin + ray * (8f + distance * (1f - MathF.Pow(1f - t, 3f)));
    public override void Update()
    {
        Position = Point(LifetimeRatio);
        Color = MostimaClockwork.TimeColor(kind == 1 ? new Color(167, 235, 255) : new Color(45, 166, 255), gold)
            * (MathHelper.Clamp(Time / 2f, 0f, 1f) * MathF.Pow(1f - LifetimeRatio, 1.4f));
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 40f)) return;
        if (kind == 1)
            SpreadVfxDetail.Facet(Position, 3f, 12f * (1f - LifetimeRatio * .7f), Rotation, Color, 1f);
        else
            MostimaClockwork.Line(Point(MathF.Max(0f, LifetimeRatio - .1f)), Position, Color, 1.8f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * .45f, Rotation,
            Texture.Size() * .5f, new Vector2(16f, 4f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
