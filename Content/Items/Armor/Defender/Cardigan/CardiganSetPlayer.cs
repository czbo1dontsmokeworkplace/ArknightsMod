using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Cardigan
{
	internal class CardiganSetPlayer : ArknightsArmorPlayer
	{
		public bool CardiganHelmetActive;
		public bool CardiganSetActive;

		public override void ResetEffects() {
			CardiganHelmetActive = false;
			CardiganSetActive = false;
		}

		public override void PostUpdateEquips() {
			CardiganHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<CardiganHelmet>());
			CardiganSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<CardiganHelmet>(),
				ModContent.ItemType<CardiganChestplate>(),
				ModContent.ItemType<CardiganGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, CardiganSetActive, "Mods.ArknightsMod.ArmorSets.Cardigan.SetBonus");

			if (CardiganSetActive)
				Player.statDefense += Player.statLifeMax2 / 40;
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (CardiganHelmetActive)
				health.Base += 40;
		}
	}
}
