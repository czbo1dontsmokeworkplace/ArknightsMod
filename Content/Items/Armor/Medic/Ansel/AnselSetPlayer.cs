using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.Ansel
{
	internal class AnselSetPlayer : ArknightsArmorPlayer
	{
		public bool AnselHelmetActive;
		public bool AnselSetActive;

		public override void ResetEffects() {
			AnselHelmetActive = false;
			AnselSetActive = false;
		}

		public override void PostUpdateEquips() {
			AnselHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<AnselHead>());
			AnselSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<AnselHead>(),
				ModContent.ItemType<AnselBody>(),
				ModContent.ItemType<AnselLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, AnselSetActive, "Mods.ArknightsMod.ArmorSets.Ansel.SetBonus");
		}
	}
}
