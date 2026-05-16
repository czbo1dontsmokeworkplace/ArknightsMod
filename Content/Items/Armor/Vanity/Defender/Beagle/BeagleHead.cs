using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Beagle
{
	[AutoloadEquip(EquipType.Head)]
    public class BeagleHead : ArknightsVanityHead
    {
		public override int Rarity => 3;
		public override int Value => 560000;
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<BeagleBody>() && legs.type == ModContent.ItemType<BeagleLegs>();
		}
	}
}
