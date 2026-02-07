using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Mlynar
{
	[AutoloadEquip(EquipType.Head)]
	public class MlynarHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void UpdateEquip(Player player) {

		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<MlynarBody>() && legs.type == ModContent.ItemType<MlynarLegs>();
		}
	}
}
