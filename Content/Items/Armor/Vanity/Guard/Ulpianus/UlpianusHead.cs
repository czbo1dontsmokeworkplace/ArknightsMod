using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Ulpianus
{
	[AutoloadEquip(EquipType.Head)]
	public class UlpianusHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void UpdateEquip(Player player) {

		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<UlpianusBody>() && legs.type == ModContent.ItemType<UlpianusLegs>();
		}
	}
}
