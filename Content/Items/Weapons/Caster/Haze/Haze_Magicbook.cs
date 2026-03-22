using ArknightsMod.Content.Projectiles.Caster.Haze;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Haze
{
	public class Haze_Magicbook : ModItem
	{
		private float Skill = 0;
		public override void SetDefaults() {
			Item.damage = 50;
			Item.knockBack = 3;
			Item.useAnimation = 48;
			Item.useTime = 48;
			Item.shootSpeed = 8;

			Item.shoot = ModContent.ProjectileType<Haze_Magicball>();
			Item.DamageType = DamageClass.Magic;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.UseSound = SoundID.Item9;
			Item.rare = ItemRarityID.Orange;
			//Item.value = Item.sellPrice(silver: 5);

			Item.noMelee = true;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			switch (Skill) {
				case 0:
					Projectile.NewProjectile(source, position, velocity * 1.5f, ModContent.ProjectileType<Haze_Magicball>(), damage, knockback);
					break;

				case 1:
					Projectile.NewProjectile(source, position, velocity * 1.5f, ModContent.ProjectileType<Haze_Crimsonball>(), damage, knockback);
					break;

				default:
					break;
			}

			return false;
		}
	}
}
