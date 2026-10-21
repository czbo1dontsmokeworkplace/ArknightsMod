using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender.Horn;

public sealed class HornBurst : ModProjectile
{
    private bool emitted;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Grenade;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 75;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 28;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (!emitted && Projectile.ai[1] == 0)
        {
            emitted = true;
            HornVisuals.Detonation(Projectile.Center, Projectile.ai[0]);
            HornVisuals.SkillDetonation(Projectile.Center, Projectile.ai[0], (int)Projectile.ai[2]);
        }
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = Projectile.ai[1] == 1;
        Lighting.AddLight(Projectile.Center, .9f * Projectile.timeLeft / 28f, .55f * Projectile.timeLeft / 28f, .15f);
    }
    public override bool ShouldUpdatePosition() => false;
    // Only the first update deals damage; the remaining lifetime is visual only.
    public override bool? CanDamage() => Projectile.timeLeft == 28;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => projHitbox.Intersects(targetHitbox);
    public override bool PreDraw(ref Color lightColor)
    {
        if (Projectile.ai[1] == 0)
        {
            HornVisuals.Explosion(Projectile.Center, 1f - Projectile.timeLeft / 28f, Projectile.ai[0]);
            HornVisuals.SkillExplosion(Projectile.Center, 1f - Projectile.timeLeft / 28f, Projectile.ai[0], (int)Projectile.ai[2]);
        }
        return false;
    }
}

public sealed class HornFlare : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Grenade;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 480;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0) Projectile.timeLeft = (int)Projectile.ai[1];
        float fade = System.Math.Min(1f, Projectile.timeLeft / 45f);
        for (int i = 0; i < 8; i++)
            Lighting.AddLight(Projectile.Center + (MathHelper.TwoPi * i / 8).ToRotationVector2() * Projectile.ai[0] * .65f,
                new Vector3(.8f, .65f, .35f) * fade);
        if (!Main.dedServ && Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.ai[0], Projectile.ai[0]),
                DustID.TintableDustLighted, new Vector2(0, -.8f), 150, HornVisuals.Gold, .7f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        HornVisuals.Flare(Projectile.Center, Projectile.ai[0], Projectile.timeLeft);
        return false;
    }
}
