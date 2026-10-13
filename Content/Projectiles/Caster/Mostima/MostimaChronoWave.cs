using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaChronoWave : ModProjectile
{
    internal const int Lifetime = 46;
    private float Age => Lifetime - Projectile.timeLeft;
    private static float RadiusAt(float age)
    {
        float t = MathHelper.Clamp(age / 13f, 0f, 1f);
        return MathHelper.Lerp(48f, 660f, 1f - MathF.Pow(1f - t, 3f));
    }
    private float Radius => RadiusAt(Age);
    public override string Texture => MostimaVisuals.Assets + "ChronoCrescentV2";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }
    // velocity stores the cast direction only. The circle center never travels with the front.
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age <= 16f ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float previous = RadiusAt(Math.Max(0f, Age - 1f));
        for (int i = 0; i <= 80; i++)
        {
            float angle = Projectile.rotation + MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, i / 80f);
            Vector2 ray = angle.ToRotationVector2();
            float point = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center + ray * previous, Projectile.Center + ray * Radius, 40f, ref point))
                return true;
        }
        return false;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        if (Main.dedServ) return;
        if (Age <= 15f && ParticleManager.activeParticles.Count < 1700)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = Projectile.rotation + MathHelper.Lerp(-1.54f, 1.54f, (i + Main.rand.NextFloat(.2f, .8f)) / 12f);
                Vector2 ray = angle.ToRotationVector2();
                Vector2 position = Projectile.Center + ray * Radius;
                new MostimaTemporalShard(position, ray * Main.rand.NextFloat(1.5f, 5f),
                    i % 3 == 0 ? new Color(222, 244, 255) : MostimaVisuals.Blue,
                    Main.rand.Next(17, 30), Main.rand.NextFloat(9f, 22f)).Spawn();
                if (i % 4 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(position, DustID.BlueCrystalShard,
                        ray * 2f, 120, MostimaVisuals.Blue, .8f);
                    dust.noGravity = true;
                }
            }
        }
        if (Age is 1f or 5f or 9f)
            MostimaVisuals.Particles(Projectile.Center, 18, 6f);
        Lighting.AddLight(Projectile.Center, .12f, .35f, .6f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!target.boss && target.realLife < 0)
            target.AddBuff(ModContent.BuffType<MostimaTemporalDrag>(), 120);
        MostimaVisuals.Particles(target.Center, 12, 6);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
        // Bounded shader ribbons run along the leading edge in five contiguous arc sections.
        if (Age <= 25f)
            for (int section = 0; section < 5; section++)
            {
                float start = Projectile.rotation - MathHelper.PiOver2 + section * MathHelper.Pi / 5f;
                Vector2 head = Projectile.Center + start.ToRotationVector2() * Radius;
                Vector2[] points = new Vector2[22];
                for (int j = 0; j < points.Length; j++)
                    points[j] = Projectile.Center + (start + (j + 1f) / points.Length * MathHelper.Pi / 5f).ToRotationVector2() * Radius;
                OperatorFlightVfx.Trail(head, points, Vector2.Zero,
                    MostimaVisuals.Blue * fade, new Color(223, 243, 255) * fade, 14f, 360f);
            }
        using var pass = MostimaVisuals.Begin();
        for (int layer = 3; layer >= 0; layer--)
        {
            float radius = RadiusAt(Math.Max(0f, Age - layer * 2.2f));
            float brightness = layer == 0 ? .73f : .24f;
            Color color = layer == 0 ? new Color(183, 227, 255)
                : layer % 2 == 0 ? MostimaVisuals.Blue : MostimaVisuals.Violet;
            MostimaVisuals.Sprite("ChronoCrescentV2", Projectile.Center,
                new Vector2(radius / .425f), color * (brightness * fade), Projectile.rotation);
        }
        for (int i = 0; i <= 12; i++)
        {
            float angle = Projectile.rotation + MathHelper.Lerp(-1.48f, 1.48f, i / 12f);
            Vector2 point = Projectile.Center + angle.ToRotationVector2() * Radius;
            MostimaVisuals.Sprite("TemporalStar", point, new Vector2(i % 3 == 0 ? 32f : 17f),
                new Color(200, 238, 255) * (.65f * fade), angle);
        }
        return false;
    }
}
