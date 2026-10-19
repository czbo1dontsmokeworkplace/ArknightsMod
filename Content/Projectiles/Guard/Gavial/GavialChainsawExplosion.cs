using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Gavial;

/// <summary>原版手榴弹范围与烟火表现；每次爆炸独立命中每个敌人一次。</summary>
public sealed class GavialChainsawExplosion : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + Terraria.ID.ProjectileID.Grenade;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 128;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0f && !Main.dedServ)
            GavialVisuals.GrenadeExplosion(Projectile.GetSource_FromThis(), Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor) => false;
}
