using Microsoft.Xna.Framework;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

public sealed class CatapultExplosion : ModProjectile
{
    internal const int Lifetime = 22;
    private int Age => Lifetime - Projectile.timeLeft;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Grenade;
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
        Projectile.width = Projectile.height = (int)(MathHelper.Clamp(Projectile.ai[0],
            ArtilleryBlast.Radius, ArtilleryBlast.Radius * 2f) * 2f);
        Projectile.Center = center;
    }
    public override bool? CanDamage() => Age <= 2 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(nearest, Projectile.Center) <= Projectile.width * Projectile.width * .25f;
    }
    public override void AI()
    {
        if (Main.dedServ || Age != 0) return;
        ArtilleryImpactVisuals.Spawn(Projectile);
        CatapultVisuals.Impact(Projectile.Center, Projectile.width * .5f);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .50f, Pitch = .18f, MaxInstances = 4 }, Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        CatapultVisuals.Explosion(Projectile.Center, Projectile.width, Age / (float)Lifetime);
        return false;
    }
}
