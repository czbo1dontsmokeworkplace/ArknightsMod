using System;
using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

/// <summary>借用原版电圈发射器火箭的贴图，飞行和命中逻辑完全独立。</summary>
public sealed class MeteoriteShell : ModProjectile
{
    private bool detonated;
    private int age;
    private int Mode => Math.Clamp((int)Projectile.ai[0], 0, 2);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ElectrosphereMissile;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 300;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 2;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        // 不施加重力；extraUpdates 只提高位移和碰撞精度，所有箭矢都是同一弹幕。
        age++;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (Main.dedServ) return;
        MeteoriteShellVisuals.SpawnFlight(Projectile, age);
        Lighting.AddLight(Projectile.Center, new Vector3(.85f, .28f, .06f) * .55f);
        if (Main.rand.NextBool(3)) return;
        Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * .4f,
            DustID.Smoke, -Projectile.velocity * .06f + Main.rand.NextVector2Circular(.6f, .6f),
            120, default, Main.rand.NextFloat(.7f, 1.1f));
        dust.noGravity = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        MeteoriteShellVisuals.DrawTrail(Projectile);
        return true;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        detonated = true;
        Projectile.Kill();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        detonated = true;
        return true;
    }

    public override void OnKill(int timeLeft)
    {
        if (!detonated || Projectile.owner != Main.myPlayer) return;
        int burstDamage = Math.Max(1, (int)(Projectile.damage * (Mode == 2 ? .9f : .68f)));
        Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<MeteoriteBurst>(), burstDamage,
            Projectile.knockBack, Projectile.owner, Mode);
        int count = Mode == 2 ? 8 : Mode == 1 ? 7 : 5;
        float phase = Main.rand.NextFloat(MathHelper.TwoPi);
        for (int i = 0; i < count; i++)
        {
            Vector2 fragmentVelocity = (phase + MathHelper.TwoPi * i / count).ToRotationVector2()
                * Main.rand.NextFloat(5.5f, 8f);
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center,
                fragmentVelocity, ModContent.ProjectileType<MeteoriteFragment>(),
                Math.Max(1, (int)(Projectile.damage * .24f)),
                Projectile.knockBack * .35f, Projectile.owner);
        }
    }
}
