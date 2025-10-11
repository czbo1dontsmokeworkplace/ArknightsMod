using ArknightsMod.Content.Items.Armor.Vanity.Guard.Oblivionis;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Indigo
{
	[AutoloadEquip(EquipType.Head)]
	public class IndigoHead : ArknightsVanityHead
	{
		public override int Rarity => 4;
		public override void UpdateEquip(Player player) {

		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<IndigoBody>() && legs.type == ModContent.ItemType<IndigoLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			
		}
	}
}
