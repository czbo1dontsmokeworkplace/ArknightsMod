using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// One frame of independent area damage; never spawns further explosions.
public sealed class GoldenglowLightningExplosion : ModProjectile
{
    public override string Texture => "ArknightsMod/Assets/Effects/Goldenglow/LightningImpact";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 75;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;
}
