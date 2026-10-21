using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Armor.Caster.Lava
{
	internal class LavaSetPlayer : ArknightsArmorPlayer
	{
		public bool LavaHelmetActive;
		public bool LavaSetActive;
		private bool initialSpAttempted;

		public override void ResetEffects() {
			LavaHelmetActive = false;
			LavaSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 LavaHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			LavaHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<LavaHead>();
			LavaSetActive = LavaHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<LavaBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<LavaLegs>();
		}

		public override void PostUpdate() {
			if (Player.dead)
				return;

			if (LavaSetActive && Player.GetModPlayer<WeaponPlayer>().SkillActive)
				Player.AddBuff(BuffID.Inferno, 2);

			if (!LavaHelmetActive || initialSpAttempted || !IsHoldingModWeapon())
				return;

			// 机会按“首次手持武器”消耗：无技能、库存已满、技能开启等导致
			// 充能失败时也不重试，脱下重戴或切换武器均不重置。
			initialSpAttempted = true;
			OperatorSPHelper.TryGainSP(Player, 15);
		}

		private bool IsHoldingModWeapon() {
			Item item = Player.HeldItem;
			return item.ModItem?.Mod == Mod
				&& (item.ModItem is UpgradeWeaponBase
					|| (item.damage > 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0));
		}

		public override void UpdateDead() => initialSpAttempted = false;

		// 复活只重置机会，等装备与武器技能初始化后由 PostUpdate 统一触发。
		public override void OnRespawn() => initialSpAttempted = false;

		// 退出重进不额外赠送同一次生命的触发机会。
		public override void SaveData(TagCompound tag) => tag["initialSpAttempted"] = initialSpAttempted;

		public override void LoadData(TagCompound tag) => initialSpAttempted = tag.GetBool("initialSpAttempted");
	}
}
