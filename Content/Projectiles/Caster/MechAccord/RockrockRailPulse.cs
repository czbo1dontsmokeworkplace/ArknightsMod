using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class RockrockRailPulse : ModProjectile
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    internal static void Spawn(IEntitySource source, Vector2 start, NPC target, int owner, int damage)
    {
        Vector2 delta = target.Center - start;
        if (delta.LengthSquared() > 440f * 440f) return;
        Projectile.NewProjectile(source, start, delta, ModContent.ProjectileType<RockrockRailPulse>(),
            damage, 0f, owner, target.whoAmI);
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.timeLeft = 12;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 10 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0] ? null : false;
    private Vector2 End => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Math.Min(440f, Projectile.velocity.Length());
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End, 6f, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        MechAccordHitFlash.Spawn(Projectile, End, true, 40f);
        MechAccordDrone.RegisterHit(Projectile.owner, target.whoAmI);
    }
    public override bool PreDraw(ref Color lightColor)
    { MechAccordVfx.DrawRail(Projectile.Center, End, Projectile.timeLeft / 12f); return false; }
}
