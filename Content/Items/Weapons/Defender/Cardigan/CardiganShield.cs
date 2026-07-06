using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
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
				// 下+右键：技能一，即时回复最大生命 40%，是瞬发技能，不应该有持续时间
				// 这里不能用 mp.SkillActive 做按键锁：Cardigan 没有在 WeaponPlayer.SetAllSkillsData 里注册专属技能数据，
				// CurrentSkill 始终是 null，导致 SkillActiveTime[Skill] 取到默认值 0，SkillActive 一旦被设为 true 就永远不会自动复位，
				// 既会在技力回充判定里被当成“技能仍在持续”从而卡住回充，也会在血量没来得及刷新前的下一帧被同一段逻辑判定为“尚未消耗”再次触发，
				// 表现为回的血瞬间又被扣掉。改用按键边沿检测，只在刚按下右键的那一帧触发一次，彻底避免重复触发。
				if (Main.myPlayer == player.whoAmI && PlayerInput.Triggers.JustPressed.MouseRight && mp.StockCount > 0) {
					// 钳制上限要用 statLifeMax2（含套装/饰品等加成后的实际生效上限），不能用 statLifeMax（基础上限）。
					// 部分干员套装只会给 statLifeMax2 加成（见 NeoArmorItem.UpdateEquip），穿着这类套装时当前血量本就高于 statLifeMax，
					// 之前钳制到 statLifeMax 会把血量瞬间砍到更低的基础上限，表现为回血瞬间又消失。
					player.statLife = Math.Min(player.statLife + (int)(player.statLifeMax * 0.4f), player.statLifeMax2);
					mp.DelStockCount();
					SoundEngine.PlaySound(SkillActiveSfx, player.Center);
				}
				return false;
			}
			return base.CanUseItem(player);
		}
	}
}
