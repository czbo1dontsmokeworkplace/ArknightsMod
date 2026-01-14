using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Surtr
{
	[AutoloadEquip(EquipType.Legs)]
	public class SurtrLegs : ArknightsVanityLegs
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {
		}
	}
}