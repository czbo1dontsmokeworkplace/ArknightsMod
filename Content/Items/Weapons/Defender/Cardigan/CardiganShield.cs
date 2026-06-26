using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender.Cardigan
{
	public class CardiganShield : ExpansionWeaponBase
	{
		// 复用原 CardiWeapon 的持握贴图
		public override string Texture => "ArknightsMod/Content/Items/Weapons/Defender/Cardigan/CardiWeapon";

		protected override int[] EliteDamage => [24, 30];

		private static SoundStyle SkillActiveSfx;

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<OrironShard>(), 1)
				.AddIngredient(ModContent.ItemType<Polyketon>(), 1)
				.AddIngredient(ModContent.ItemType<Device>(), 1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.Register();
		}

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
			Item.crit = 4;
			// 攻击表现完全交给常驻的剑/盾弹幕处理
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.useStyle = ItemUseStyleID.HiddenAnimation;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			var mp = player.GetModPlayer<WeaponPlayer>();
			if (player.altFunctionUse == 2 && player.controlDown) {
				// 下+右键：技能一，即时回复最大生命 40%
				// 用 mp.SkillActive 做按键状态锁，避免组合键持续按住时每帧重复触发回血/扣血（导致回的血又被扣回去）
				if (mp.StockCount > 0 && !mp.SkillActive) {
					mp.SkillActive = true;
					player.statLife = Math.Min(player.statLife + (int)(player.statLifeMax * 0.4f), player.statLifeMax);
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}
			mp.SkillActive = false;
			return base.CanUseItem(player);
		}
	}
}
