using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Sussurro;

// Retained registered type for compatibility; the removed right-click attack cannot run.
public sealed class SussurroStaffHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.EmeraldStaff;
    public override void SetDefaults() { Projectile.timeLeft = 1; Projectile.tileCollide = false; }
    public override bool? CanDamage() => false;
    public override void AI() => Projectile.Kill();
    public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor) => false;
}
