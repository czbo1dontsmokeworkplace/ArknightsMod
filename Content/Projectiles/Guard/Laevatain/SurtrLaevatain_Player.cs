using ArknightsMod.Content.Items.Weapons.Guard.Surtr;
using ArknightsMod.Content.Projectiles.Guard.Frostleaf;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Laevatain
{
	public class SurtrLaevatain_Player : ModPlayer
	{
		public override void PostUpdate() {
			if (Player.HeldItem.type == ModContent.ItemType<SurtrLaevatain>()) {
				if (Player.ownedProjectileCounts[ModContent.ProjectileType<SurtrLaevatain_Proj>()] == 0) {
					Projectile.NewProjectile(Player.GetSource_FromThis(),Player.MountedCenter-Main.screenPosition,Vector2.One,ModContent.ProjectileType<SurtrLaevatain_Proj>()
						,Player.HeldItem.damage,Player.HeldItem.knockBack);
				}
			}
		}
	}
}

