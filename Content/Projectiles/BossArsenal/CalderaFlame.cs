using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed class CalderaFlame : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;
    public override void SetDefaults() {
        Projectile.CloneDefaults(ProjectileID.Flames);
        AIType = ProjectileID.Flames;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public override bool PreDraw(ref Color lightColor) { BossArsenalPolishVisuals.VanillaFlame(Projectile); return false; }
}
