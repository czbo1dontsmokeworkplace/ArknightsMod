using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Provence
{
	[AutoloadEquip(EquipType.Head)]
	internal class ProvenceHead:ArknightsVanityHead
	{
		public override int Rarity => 5;
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ProvenceBody>() && legs.type == ModContent.ItemType<ProvenceLegs>();
		}
	}
}
