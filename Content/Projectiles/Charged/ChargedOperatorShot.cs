using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

public sealed class ChargedOperatorShot : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private int Skill => (int)Projectile.ai[2];
    private float distance;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 24; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 300; Projectile.extraUpdates = 4; Projectile.penetrate = 4;
        Projectile.ignoreWater = true; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Projectile.velocity *= 1.2f;
        Projectile.penetrate = 4;
        Projectile.tileCollide = !(Kind == ChargedOperatorKind.Ambriel && Skill == 2);
        int size = 8;
        Vector2 center = Projectile.Center; Projectile.width = Projectile.height = size; Projectile.Center = center;
    }
    public override void AI()
    {
        distance += Projectile.velocity.Length();
        if (distance >= ChargedOperatorRules.ShotRange(Kind, Skill)) { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0 && !Main.dedServ)
        {
            Color color = ChargedOperatorRules.ColorFor(Kind);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * .35f);
            ChargedSniperVisuals.BulletParticles(Projectile, Kind);
        }
    }
    public override bool? CanHitNPC(NPC target) => Kind == ChargedOperatorKind.Andreana && Skill == 2 && target.life <= target.lifeMax * .5f ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, 8, ref point);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == ChargedOperatorKind.Fartooth)
        {
            if (Skill == 3 && distance > 800) modifiers.SourceDamage *= 1.4f;
            if (Skill == 3) modifiers.ArmorPenetration += 35;
        }
        if (Kind == ChargedOperatorKind.Firewatch && target.damage == 0) modifiers.SourceDamage *= 1.25f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == ChargedOperatorKind.Ambriel && Skill == 1) OperatorStunNPC.TryApply(target, 36);
        if (Kind == ChargedOperatorKind.Andreana && Skill == 2) OperatorStunNPC.TryApply(target, 60);
        ChargedSniperVisuals.HitBurst(target.Hitbox.ClosestPointInRect(Projectile.Center), Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, false, hit.Crit);
    }
    public override void OnKill(int timeLeft) => ChargedOperatorVfx.Burst(Projectile.Center, Kind, 6, 18);
    public override bool PreDraw(ref Color lightColor)
    {
        ChargedSniperVisuals.DrawBullet(Projectile, Kind);
        return false;
    }
}

public sealed class DreadnoughtFragment : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 16; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1; Projectile.timeLeft = 40; Projectile.ignoreWater = true;
    }
    public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[1] && Projectile.timeLeft > 30 ? false : null;
    public override void AI()
    {
        if (Kind == ChargedOperatorKind.NearlRadiant && Projectile.timeLeft < 30)
        {
            NPC target = Projectile.FindTargetWithinRange(280, true);
            if (target != null) Projectile.velocity = Vector2.Lerp(Projectile.velocity, (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 12, .08f);
        }
        else if (Kind == ChargedOperatorKind.Matoimaru) Projectile.velocity.Y -= .1f;
        else if (Kind == ChargedOperatorKind.Franka) Projectile.velocity *= 1.01f;
        else Projectile.velocity *= .98f;
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { if (Kind == ChargedOperatorKind.Franka) target.AddBuff(BuffID.OnFire3, 120); }
    public override void OnKill(int timeLeft) => ChargedOperatorVfx.Burst(Projectile.Center, Kind, 5, 16);
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = ChargedOperatorRules.ColorFor(Kind);
        ChargedOperatorVfx.Trail(Projectile, color, 8);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite(Kind == ChargedOperatorKind.Melantha ? "ImpactPetals" : "ForwardFlame", Projectile.Center,
            new Vector2(32, 16), color * Math.Min(1, Projectile.timeLeft / 10f), Projectile.rotation);
        return false;
    }
}
