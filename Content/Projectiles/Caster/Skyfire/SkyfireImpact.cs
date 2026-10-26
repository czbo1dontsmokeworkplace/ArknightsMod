using System;
using ArknightsMod.Common.VisualEffects.HelldiverExplosions;
using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;

namespace ArknightsMod.Content.Projectiles.Caster.Skyfire;

/// <summary>火属性范围伤害和短暂爆炸判定；后续余焰只绘制，不重复伤害。</summary>
public sealed class SkyfireImpact : ModProjectile
{
    private const int Lifetime = 25;
    private bool Skyfall => Projectile.ai[0] > 0f;
    private float Age => Projectile.localAI[0];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 340;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        HelldiverExplosion.Spawn(Projectile, Projectile.Center, 7);
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = Skyfall ? 470 : 340;
        Projectile.Center = center;
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<SpreadAfterfield>(), Math.Max(1, (int)(Projectile.damage * .30f)),
                Projectile.knockBack, Projectile.owner, 3f);
    }

    public override bool? CanDamage() => Age <= 2f ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = new(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
            MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
        return Vector2.DistanceSquared(Projectile.Center, nearest) <= Projectile.width * Projectile.width * .25f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, Skyfall ? 8 * 60 : 5 * 60);
        if (Skyfall) OperatorStunNPC.TryApply(target, 60);
    }

    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0f || Main.dedServ) return;
        SkyfireVisuals.ImpactParticles(Projectile.Center, Skyfall);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}
