using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Endfield.Striker.Yvonne
{
	[AutoloadEquip(EquipType.Head)]
	public class YvonneHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<YvonneBody>() &&
				legs.type == ModContent.ItemType<YvonneLegs>();
		}
	}
}
