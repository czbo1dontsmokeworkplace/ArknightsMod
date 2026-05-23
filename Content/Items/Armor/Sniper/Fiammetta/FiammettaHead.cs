using Terraria.ModLoader;
using Terraria;
namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	[AutoloadEquip(EquipType.Head)]
	public class FiammettaHead : NeoArmorHead
	{
		public override int Rarity => 6;
		public override void Load() {
		}
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<FiammettaBody>() && legs.type == ModContent.ItemType<FiammettaLegs>();
		}

	}

}