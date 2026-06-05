using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Beagle
{
	internal class BeagleSetPlayer : ArknightsArmorPlayer
	{
		public bool BeagleHelmetActive;
		public bool BeagleSetActive;

		public override void ResetEffects() {
			BeagleHelmetActive = false;
			BeagleSetActive = false;
		}

		public override void PostUpdateEquips() {
			BeagleHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<BeagleHelmet>());
			BeagleSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<BeagleHelmet>(),
				ModContent.ItemType<BeagleChestplate>(),
				ModContent.ItemType<BeagleGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, BeagleSetActive, "Mods.ArknightsMod.ArmorSets.Beagle.SetBonus");

			if (BeagleSetActive) {
				int emptySlots = CountEmptyAccessorySlots();
				int bonusSlots = System.Math.Min(emptySlots, 3);
				Player.statDefense += 6 + bonusSlots * 6;
			}
		}

		public override void PostUpdate() {
			if (BeagleHelmetActive)
				Player.noKnockback = true;
		}

		private int CountEmptyAccessorySlots() {
			int count = 0;
			int accessorySlots = 5 + (Player.extraAccessory ? 1 : 0);
			for (int i = 3; i < 3 + accessorySlots; i++) {
				if (Player.armor[i].IsAir)
					count++;
			}

			return count;
		}
	}
}
