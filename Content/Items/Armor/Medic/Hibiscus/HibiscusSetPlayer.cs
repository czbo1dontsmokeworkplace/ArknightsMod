using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Hibiscus
{
	internal class HibiscusSetPlayer : ArknightsArmorPlayer
	{
		public bool HibiscusHelmetActive;
		public bool HibiscusSetActive;

		public override void ResetEffects() {
			HibiscusHelmetActive = false;
			HibiscusSetActive = false;
		}

		public override void PostUpdateEquips() {
			HibiscusHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<HibiscusHelmet>());
			HibiscusSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<HibiscusHelmet>(),
				ModContent.ItemType<HibiscusChestplate>(),
				ModContent.ItemType<HibiscusGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, HibiscusSetActive, "Mods.ArknightsMod.ArmorSets.Hibiscus.SetBonus");
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (HibiscusSetActive)
				health.Base += 30;
		}
	}
}
