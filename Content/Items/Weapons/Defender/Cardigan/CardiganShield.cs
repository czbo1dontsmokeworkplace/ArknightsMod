using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Projectiles.Defender.Cardigan;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender.Cardigan
{
	public class CardiganShield : ExpansionWeaponBase
	{
		protected override int[] EliteDamage => [24, 30];

		private static SoundStyle SkillActiveSfx;

		public override void Load() {
			SkillActiveSfx = new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.5f, MaxInstances = 2 };
		}

		public override void SetDefaults() {
			Item.damage = EliteDamage[0];
			Item.DamageType = DamageClass.Melee;
			Item.width = 48;
			Item.height = 46;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.knockBack = 7f;
			Item.value = Item.sellPrice(silver: 15);
			Item.rare = ItemRarityID.Blue;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<CardiganShieldBash>();
			Item.shootSpeed = 18f;
			Item.crit = 4;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (player.altFunctionUse == 2) {
				// 技能一：即时回复最大生命 40%
				if (mp.StockCount > 0) {
					player.statLife = Math.Min(player.statLife + (int)(player.statLifeMax * 0.4f), player.statLifeMax);
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}
			// 同一时间只允许一个推击弹幕存在
			return player.ownedProjectileCounts[ModContent.ProjectileType<CardiganShieldBash>()] <= 0
				&& base.CanUseItem(player);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX);
			Projectile.NewProjectile(source, player.Center + dir * 16f, dir * Item.shootSpeed,
				ModContent.ProjectileType<CardiganShieldBash>(), damage, knockback, player.whoAmI);
			return false;
		}
	}
}
