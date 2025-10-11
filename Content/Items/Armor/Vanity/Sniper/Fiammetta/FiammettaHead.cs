using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Terraria.GameContent.Creative;
namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Fiammetta
{
	[AutoloadEquip(EquipType.Head)]
	public class FiammettaHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {
		}
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<FiammettaBody>() && legs.type == ModContent.ItemType<FiammettaLegs>();
		}

	}

}