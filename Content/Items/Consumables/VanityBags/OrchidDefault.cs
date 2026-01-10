using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Supporter.Orchid;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class OrchidDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<OrchidHead>(),
			ModContent.ItemType<OrchidBody>(),
			ModContent.ItemType<OrchidLegs>()
		];
		}
	}
}
