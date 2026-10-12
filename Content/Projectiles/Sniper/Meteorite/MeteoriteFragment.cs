using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

/// <summary>爆炸破片短距离直线飞行，初始几帧不重复打中中心目标。</summary>
public sealed class MeteoriteFragment : ModProjectile
{
    private int age;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 30;
        Projectile.extraUpdates = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.scale = .65f;
    }

    public override bool? CanDamage() => age >= 10 ? null : false;

    public override void AI()
    {
        age++;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Projectile.velocity *= .994f;
        if (!Main.dedServ && Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                -Projectile.velocity * .08f, 130, default, .65f);
            dust.noGravity = true;
        }
    }
}
