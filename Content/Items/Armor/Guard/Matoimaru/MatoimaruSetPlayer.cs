using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Matoimaru
{
	internal class MatoimaruSetPlayer : ArknightsArmorPlayer
	{
		public bool MatoimaruHelmetActive;
		public bool MatoimaruSetActive;

		public override void ResetEffects() {
			MatoimaruHelmetActive = false;
			MatoimaruSetActive = false;
		}

		public override void PostUpdateEquips() {
			MatoimaruHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MatoimaruHelmet>());
			MatoimaruSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MatoimaruHelmet>(),
				ModContent.ItemType<MatoimaruChestplate>(),
				ModContent.ItemType<MatoimaruGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MatoimaruSetActive, "Mods.ArknightsMod.ArmorSets.Matoimaru.SetBonus");

			if (MatoimaruSetActive)
				extraDefenseBonus -= 0.2f;
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (MatoimaruSetActive)
				health *= 1.3f;
		}

		public override void GetHealLife(Item item, bool quickHeal, ref int healValue) {
			if (MatoimaruHelmetActive)
				healValue = (int)(healValue * 1.25f);
		}
	}
}
