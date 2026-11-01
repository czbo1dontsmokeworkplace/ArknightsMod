using System;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	internal class LingSetPlayer : ArknightsArmorPlayer
	{
		public bool LingHelmetActive;
		public bool LingSetActive;

		public int SummonDamageStacks;
		private float lastUnusedSlots = -1f;
		private float releasedSlotRemainder;

		public override void ResetEffects() {
			LingHelmetActive = false;
			LingSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 LingHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			LingHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<LingHead>();
			LingSetActive = LingHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<LingBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<LingLegs>();

			if (LingHelmetActive)
				Player.maxMinions++;

			if (LingSetActive) {
				Player.maxMinions++;
				Player.GetModPlayer<WeaponPlayer>().SPRegenMultiplier += 0.2f * SummonDamageStacks;
			}
		}

		private void ApplyArmorDamageStats() {

			float unused = OperatorMinionSlotHelper.CountUnusedMinionSlots(Player);
			if (LingHelmetActive && unused > 0)
				Player.GetDamage(DamageClass.Summon) *= 1f + 0.2f * unused;

			if (LingSetActive && SummonDamageStacks > 0)
				Player.GetDamage(DamageClass.Summon) *= 1f + 0.05f * SummonDamageStacks;
		}

		public override void PostUpdate() {
			if (!LingSetActive) {
				lastUnusedSlots = -1;
				releasedSlotRemainder = 0f;
				return;
			}

			float unused = OperatorMinionSlotHelper.CountUnusedMinionSlots(Player);
			if (lastUnusedSlots >= 0 && unused > lastUnusedSlots) {
				// 小数栏位释放量累计满1 slot才叠层，不把每个召唤物当成一栏。
				releasedSlotRemainder += unused - lastUnusedSlots;
				int gained = (int)Math.Floor(releasedSlotRemainder + 0.00001f);
				releasedSlotRemainder = Math.Max(0f, releasedSlotRemainder - gained);
				SummonDamageStacks = Math.Min(5, SummonDamageStacks + gained);
			}

			lastUnusedSlots = unused;
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorDamageStats();
		}
	}
}
