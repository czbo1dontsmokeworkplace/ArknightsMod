using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// One direct-contact blast, using the held or thrown anchor's snapshotted damage. The
// later light, ring and fragments are cosmetic and cannot repeat the damage.
public sealed class UlpianusAnchorExplosion : ModProjectile
{
    internal const int Size = 150;
    internal const int Lifetime = 30;
    internal static void Release(Projectile anchor, Vector2 point)
    {
        if (anchor.owner != Main.myPlayer) return;
        int index = Projectile.NewProjectile(anchor.GetSource_FromThis(), point, Vector2.Zero,
            ModContent.ProjectileType<UlpianusAnchorExplosion>(), anchor.damage, anchor.knockBack, anchor.owner);
        if ((uint)index < Main.maxProjectiles) {
            Main.projectile[index].CritChance = anchor.CritChance;
            Main.projectile[index].netUpdate = true;
        }
    }
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
