using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaChronoWave : ModProjectile
{
    private float Age => 55 - Projectile.timeLeft;
    private float Radius => MathHelper.Lerp(75, 230, MathHelper.Clamp(Age / 35, 0, 1));
    public override string Texture => MostimaVisuals.Assets + "ChronoDial";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 20;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 55;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Match the forward semicircular wavefront. Swept samples prevent thin targets slipping through.
        Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i <= 20; i++)
        {
            Vector2 p = Projectile.Center + forward.RotatedBy(MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, i / 20f)) * Radius;
            if (Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(p), p) <= 32 * 32) return true;
        }
        return false;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        // Two counter-phase helices: deterministic trajectory with restrained random spark scatter.
        if ((int)Age % 2 == 0)
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 offset = new Vector2(0, MathF.Sin(Age * .36f) * Radius * .65f * side).RotatedBy(Projectile.rotation);
                new MostimaChronoParticle(Projectile.Center + offset, -Projectile.velocity * .13f,
                    side < 0 ? MostimaVisuals.Blue : MostimaVisuals.Violet, 24).Spawn();
            }
        if ((int)Age % 4 == 0)
        {
            Vector2 edge = Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-1.5f, 1.5f)) * Radius;
            Dust dust = Dust.NewDustPerfect(edge, DustID.BlueCrystalShard, -Projectile.velocity * .12f, 140,
                MostimaVisuals.Blue, .75f);
            dust.noGravity = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!target.boss && target.realLife < 0) target.AddBuff(ModContent.BuffType<MostimaTemporalDrag>(), 120);
        MostimaVisuals.Particles(target.Center, 8, 5);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Projectile.timeLeft / 14f, 0, 1);
        OperatorFlightVfx.Trail(Projectile, MostimaVisuals.Violet * fade, MostimaVisuals.Blue * fade, 25, 240);
        using var pass = MostimaVisuals.Begin();
        MostimaVisuals.Sprite("ChronoDial", Projectile.Center, new Vector2(Radius * 2), MostimaVisuals.Blue * fade * .8f, Projectile.rotation);
        MostimaVisuals.Sprite("ChronoDial", Projectile.Center - Projectile.velocity * 1.8f,
            new Vector2(Radius * 1.83f), MostimaVisuals.Violet * fade * .4f, Projectile.rotation);
        for (int i = 0; i < 5; i++)
        {
            float angle = Projectile.rotation + MathHelper.Lerp(-1.3f, 1.3f, i / 4f);
            MostimaVisuals.Sprite("ChronoCore", Projectile.Center + angle.ToRotationVector2() * Radius,
                new Vector2(16), MostimaVisuals.Blue * fade * .8f, angle);
        }
        return false;
    }
}
