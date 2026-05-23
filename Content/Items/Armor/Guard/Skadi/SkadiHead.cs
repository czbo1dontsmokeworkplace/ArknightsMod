using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Guard.Skadi
{
	[AutoloadEquip(EquipType.Head)]
	public class SkadiHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<SkadiBody>() && legs.type == ModContent.ItemType<SkadiLegs>();
		}
	}
}
