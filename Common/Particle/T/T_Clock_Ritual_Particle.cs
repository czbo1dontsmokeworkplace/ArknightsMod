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

public sealed class T_Clock_Ritual_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center;
    private readonly float angle;
    private readonly int kind;
    private readonly bool alternate;
    private readonly float scale, gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    internal T_Clock_Ritual_Particle(Vector2 center, float angle, int kind, bool alternate, float scale = 1f, float gold = 0f)
    {
        this.center = center; this.angle = angle; this.kind = kind; this.alternate = alternate; this.scale = scale; this.gold = gold;
        Lifetime = kind == 0 ? 26 : kind == 1 ? 22 : 30;
        Position = Point(0);
    }
    private Vector2 Point(float t)
    {
        float a = angle + (alternate ? 1f : -1f) * (kind == 1 ? .85f : .22f) * t;
        float radius = kind == 0 ? 80f + 275f * (1f - MathF.Pow(1f - t, 3f))
            : kind == 1 ? 280f - 215f * t : 295f + 65f * MathF.Sin(t * MathHelper.PiOver2);
        return center + a.ToRotationVector2() * radius * scale;
    }
    public override void Update()
    {
        Position = Point(LifetimeRatio); Rotation = angle + LifetimeRatio * (alternate ? 1 : -1);
        float fade = MathHelper.Clamp(Time / 3f, 0f, 1f) * MathF.Pow(1f - LifetimeRatio, .9f);
        Color = MostimaClockwork.TimeColor(kind == 2 ? MostimaClockwork.Brass : alternate ? MostimaVisuals.Blue : MostimaVisuals.Violet, gold) * fade;
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 80f)) return;
        if (kind == 0)
            SpreadVfxDetail.Facet(Position, 7f, 23f * (1f - LifetimeRatio * .6f), Rotation, Color, 1f);
        else if (kind == 1)
        {
            Vector2 previous = Position;
            for (int i = 1; i <= 9; i++)
            {
                Vector2 next = Point(MathF.Max(0f, LifetimeRatio - i * .014f));
                MostimaClockwork.Line(previous, next, OperatorFlightVfx.Add(Color, 1f - i / 10f), 3f - i * .25f);
                previous = next;
            }
        }
        else
        {
            Vector2 ray = Rotation.ToRotationVector2(), side = new(-ray.Y, ray.X);
            MostimaClockwork.Line(Position - ray * 9f, Position + ray * 9f, Color, 2f);
            MostimaClockwork.Line(Position - side * 5f, Position + side * 5f, Color, 1.5f);
            MostimaClockwork.Line(Position + ray * 9f, Position + ray * 3f + side * 6f, Color, 1.5f);
        }
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, OperatorFlightVfx.Add(Color, .5f), 0f,
            Texture.Size() * .5f, new Vector2(kind == 2 ? 22f : 15f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
