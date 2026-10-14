using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// A synchronized, non-damaging event at the true hit location. Only local visual
// systems own the emitted particles; none follows the clock or NPC afterwards.
public sealed class MostimaClockHit : ModProjectile
{
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 24;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void AI()
    {
        if (Main.dedServ || Projectile.localAI[0] != 0f) return;
        Projectile.localAI[0] = 1f;
        MostimaGrandClockVfx.Hit(Projectile.Center, Projectile.ai[0], Projectile.ai[1]);
    }
}

public sealed class MostimaHitShard : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, ray;
    private readonly float distance;
    private readonly int kind;
    private readonly float gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal MostimaHitShard(Vector2 origin, float angle, float size, int kind, float gold = 0f)
    {
        Position = this.origin = origin; ray = angle.ToRotationVector2();
        this.kind = kind; this.gold = gold; Rotation = angle;
        distance = MathHelper.Clamp(size, 16f, 64f) + 48f + kind * 15f;
        Lifetime = 18 + kind * 3;
    }
    private Vector2 Point(float t) => origin + ray * (8f + distance * (1f - MathF.Pow(1f - t, 3f)));
    public override void Update()
    {
        Position = Point(LifetimeRatio);
        Color = MostimaClockwork.TimeColor(kind == 1 ? MostimaVisuals.Violet : MostimaVisuals.Blue, gold)
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
