using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Durin
{
	public class DurinWand_Projectile : ModProjectile
	{

		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;

			Projectile.aiStyle = 0;
			Projectile.friendly = true;
			Projectile.penetrate = 1; // How many monsters the projectile can penetrate.
			Projectile.tileCollide = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.ownerHitCheck = true; // Prevents hits through tiles. Most melee weapons that use projectiles have this

			AIType = ProjectileID.EnchantedBoomerang;
		}

		public override Color? GetAlpha(Color lightColor) {
			// return Color.White;
			return new Color(255, 255, 255, 0) * Projectile.Opacity;
		}

		public override void AI() {
			if (Projectile.ai[0] == 1f)
			{
				Vector2 toMouse = Main.MouseWorld - Projectile.Center;
				Projectile.rotation = toMouse.ToRotation();
			}
			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] >= 50f)
				Projectile.Kill();
		}

	}


}
