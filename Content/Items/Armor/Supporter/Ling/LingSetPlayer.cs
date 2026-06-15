using System;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	internal class LingSetPlayer : ArknightsArmorPlayer
	{
		public bool LingHelmetActive;
		public bool LingSetActive;

		public int SummonDamageStacks;
		private int lastUnusedSlots = -1;

		public override void ResetEffects() {
			LingHelmetActive = false;
			LingSetActive = false;
		}

		public override void PostUpdateEquips() {
			LingHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<LingHelmet>());
			LingSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<LingHelmet>(),
				ModContent.ItemType<LingChestplate>(),
				ModContent.ItemType<LingGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, LingSetActive, "Mods.ArknightsMod.ArmorSets.Ling.SetBonus");

			if (LingHelmetActive)
				Player.maxMinions++;

			if (LingSetActive)
				Player.maxMinions++;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!item.DamageType.CountsAsClass(DamageClass.Summon))
				return;

			int unused = OperatorMinionSlotHelper.CountUnusedMinionSlots(Player);
			if (LingHelmetActive && unused > 0)
				damage *= 1f + 0.2f * unused;

			if (LingSetActive && SummonDamageStacks > 0)
				damage *= 1f + 0.05f * SummonDamageStacks;
		}

		public override void PostUpdate() {
			if (!LingSetActive) {
				lastUnusedSlots = -1;
				return;
			}

			int unused = OperatorMinionSlotHelper.CountUnusedMinionSlots(Player);
			if (lastUnusedSlots >= 0 && unused > lastUnusedSlots) {
				int gained = unused - lastUnusedSlots;
				for (int i = 0; i < gained; i++) {
					if (SummonDamageStacks < 5)
						SummonDamageStacks++;
					OperatorSPHelper.TryGainSP(Player, 2);
				}
			}

			lastUnusedSlots = unused;
		}
	}
}
