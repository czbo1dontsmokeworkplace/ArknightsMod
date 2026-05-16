using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle
{
	[AutoloadEquip(EquipType.Legs)]
	public class BeagleLegs : ArknightsVanityLegs
    {
		public override int Rarity => 3;
		public override int Value => 560000;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {

		}
	}
}
