using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Bagpipe;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class BagpipeDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<BagpipeHead>(),
			ModContent.ItemType<BagpipeBody>(),
			ModContent.ItemType<BagpipeLegs>()
		];
		}
	}
}
