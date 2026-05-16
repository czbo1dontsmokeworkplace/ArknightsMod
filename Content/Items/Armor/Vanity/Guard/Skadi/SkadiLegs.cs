using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Skadi
{
	[AutoloadEquip(EquipType.Legs)]
	public class SkadiLegs : ArknightsVanityLegs
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {
		}
	}
}