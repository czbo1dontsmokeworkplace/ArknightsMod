using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Saria
{
	[AutoloadEquip(EquipType.Head)]
	public class SariaHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<SariaBody>() && legs.type == ModContent.ItemType<SariaLegs>();
		}
	}
	
}
