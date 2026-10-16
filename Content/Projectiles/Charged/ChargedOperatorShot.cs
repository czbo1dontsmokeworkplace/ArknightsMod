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
    private bool hitBurst;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 24; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 300; Projectile.extraUpdates = 2; Projectile.penetrate = 6;
        Projectile.ignoreWater = true; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Projectile.penetrate = 2 + (int)(4 * Projectile.ai[1]);
        Projectile.tileCollide = !(Kind == ChargedOperatorKind.Ambriel && Skill == 2);
        int size = (int)ChargedOperatorRules.ShotWidth(Projectile.ai[1]);
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
            if (Projectile.timeLeft % 6 == 0) ChargedOperatorVfx.Sparkle(Projectile.Center, -Projectile.velocity * .03f, color, Projectile.rotation, .7f);
        }
    }
    public override bool? CanHitNPC(NPC target) => Kind == ChargedOperatorKind.Andreana && Skill == 2 && target.life <= target.lifeMax * .5f ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, ChargedOperatorRules.ShotWidth(Projectile.ai[1]), ref point);
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
        ChargedOperatorVfx.Burst(target.Hitbox.ClosestPointInRect(Projectile.Center), Kind, 10, 30);
        // One impact per shot; piercing cannot multiply the large burst without bound.
        if (!hitBurst && Projectile.ai[1] >= 1 && Projectile.owner == Main.myPlayer)
        {
            hitBurst = true;
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ChargedOperatorImpact>(), Math.Max(1, Projectile.damage / 5), 2,
                Projectile.owner, (int)Kind, 48);
        }
    }
    public override void OnKill(int timeLeft) => ChargedOperatorVfx.Burst(Projectile.Center, Kind, 6, 18);
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = ChargedOperatorRules.ColorFor(Kind);
        ChargedOperatorVfx.Trail(Projectile, color, ChargedOperatorRules.ShotWidth(Projectile.ai[1]) * .65f);
        using var pass = new ChargedOperatorVfx.Pass(true);
        string shape = Kind switch { ChargedOperatorKind.Andreana => "CrystalVortex", ChargedOperatorKind.Firewatch => "ImpactPetals", _ => "StarCore" };
        ChargedOperatorVfx.Sprite(shape, Projectile.Center, new Vector2(38 + 30 * Projectile.ai[1], 12 + 16 * Projectile.ai[1]), color * .85f, Projectile.rotation);
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
