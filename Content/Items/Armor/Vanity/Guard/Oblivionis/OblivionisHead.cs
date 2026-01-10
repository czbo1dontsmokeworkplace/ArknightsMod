using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Oblivionis
{
	[AutoloadEquip(EquipType.Head)]
	public class OblivionisHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		public override int Value => 560000;
		public override void UpdateEquip(Player player) {

		}
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<OblivionisBody>() && legs.type == ModContent.ItemType<OblivionisLegs>();
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "Gokiganyou~Doktah";
		}
	}
	
}
