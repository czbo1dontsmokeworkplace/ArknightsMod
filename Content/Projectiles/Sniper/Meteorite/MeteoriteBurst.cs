using System;
using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

/// <summary>陨星专用的范围伤害，复用本模组菲亚梅塔的灰度爆破模板。</summary>
public sealed class MeteoriteBurst : ModProjectile
{
    private const int Lifetime = 24;
    private int Age => Lifetime - Projectile.timeLeft;
    private int Mode => Math.Clamp((int)Projectile.ai[0], 0, 2);
    private float Radius => Projectile.width * .5f;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = ArtilleryBlast.Diameter;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = Mode switch { 1 => 340, 2 => 420, _ => ArtilleryBlast.Diameter };
        Projectile.Center = center;
    }

    public override bool? CanDamage() => Age <= 2 ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
        float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
        return Vector2.DistanceSquared(Projectile.Center, new Vector2(x, y)) <= Radius * Radius;
    }

    public override void AI()
    {
        if (Age != 0 || Main.dedServ) return;
        ArtilleryImpactVisuals.Spawn(Projectile);
        MeteoriteBurstVisuals.DustBurst(Projectile.Center, Radius);
        SoundEngine.PlaySound(SoundID.Item14 with
        { Volume = Mode == 2 ? .77f : .58f, Pitch = Mode == 2 ? -.25f : -.04f,
          PitchVariance = .08f, MaxInstances = 6 }, Projectile.Center);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Mode == 2) target.AddBuff(BuffID.BrokenArmor, 8 * 60);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        MeteoriteBurstVisuals.Draw(Projectile.Center, Projectile.width,
            MathHelper.Clamp(Age / (float)Lifetime, 0f, 1f));
        return false;
    }
}
