using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Supporter.Ling
{
	[AutoloadEquip(EquipType.Head)]
	public class LingHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override void Load() {
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<LingBody>() && legs.type == ModContent.ItemType<LingLegs>();
		}
	}
}
