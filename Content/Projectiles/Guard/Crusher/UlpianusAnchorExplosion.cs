using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// One direct-contact blast, using the thrown anchor's snapshotted damage. The
// later light, ring and fragments are cosmetic and cannot repeat the damage.
public sealed class UlpianusAnchorExplosion : ModProjectile
{
    internal const int Size = 150;
    internal const int Lifetime = 30;
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = Size;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Lifetime; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanDamage() => Projectile.ai[0] <= 1 ? null : false;
    public override void AI()
    {
        if (Projectile.ai[0]++ == 0) UlpianusExplosionVisuals.Burst(Projectile);
        UlpianusExplosionVisuals.Light(Projectile);
    }
    public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
    {
        UlpianusExplosionVisuals.Draw(Projectile);
        return false;
    }
}
