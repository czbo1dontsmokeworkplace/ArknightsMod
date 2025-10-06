using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Fiammetta;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class FiammettaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<FiammettaHead>(),
			ModContent.ItemType<FiammettaBody>(),
			ModContent.ItemType<FiammettaLegs>()
		};
		}
	}
}
