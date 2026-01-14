using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Fiammetta;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class FiammettaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<FiammettaHead>(),
			ModContent.ItemType<FiammettaBody>(),
			ModContent.ItemType<FiammettaLegs>()
		];
		}
	}
}
