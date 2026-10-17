using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Charged;

// A stationary, single-hit beam. ai slots match ChargedOperatorShot: operator, charge, skill.
public sealed class ChargedOperatorLaser : ModProjectile
{
    public const int Lifetime = 18;
    public override string Texture => "ArknightsMod/Content/Textures/ChargedSniper/UltimaRayMid";
    private ChargedOperatorKind Kind => (ChargedOperatorKind)(int)Projectile.ai[0];
    private int Skill => (int)Projectile.ai[2];
    private float Length => Projectile.localAI[0];
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3400;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source) => Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.scale = 2.1f * Projectile.timeLeft / Lifetime;
        if (Projectile.localAI[1] == 0)
        {
            Projectile.localAI[1] = 1;
            Projectile.localAI[0] = ChargedSniperVisuals.ScanLength(Projectile.Center, Projectile.velocity, Kind, Skill);
            ChargedSniperVisuals.BeamParticles(Projectile.Center, Projectile.velocity, Length, Kind);
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Length <= 0) return false;
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity * Length, 16 * Projectile.scale, ref point);
    }
    public override bool? CanHitNPC(NPC target) => Kind == ChargedOperatorKind.Andreana && Skill == 2
        && target.life <= target.lifeMax * .5f ? false : null;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == ChargedOperatorKind.Fartooth && Skill == 3)
        {
            modifiers.ArmorPenetration += 35;
            if (Vector2.Dot(target.Center - Projectile.Center, Projectile.velocity) > 800) modifiers.SourceDamage *= 1.4f;
        }
        if (Kind == ChargedOperatorKind.Firewatch && target.damage == 0) modifiers.SourceDamage *= 1.25f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == ChargedOperatorKind.Ambriel && Skill == 1) OperatorStunNPC.TryApply(target, 36);
        if (Kind == ChargedOperatorKind.Andreana && Skill == 2) OperatorStunNPC.TryApply(target, 60);
        float along = Math.Clamp(Vector2.Dot(target.Center - Projectile.Center, Projectile.velocity), 0, Length);
        ChargedSniperVisuals.HitBurst(Projectile.Center + Projectile.velocity * along, Projectile.velocity, Kind, true, hit.Crit);
    }
    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Length, 16 * Projectile.scale, DelegateMethods.CutTiles);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        ChargedSniperVisuals.DrawBeam(Projectile.Center, Projectile.velocity, Length, Projectile.scale,
            ChargedOperatorRules.ColorFor(Kind), Projectile.timeLeft / (float)Lifetime);
        return false;
    }
}
