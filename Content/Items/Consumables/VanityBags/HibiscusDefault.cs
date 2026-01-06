using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Medic.Hibiscus;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class HibiscusDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<HibiscusHead>(),
			ModContent.ItemType<HibiscusBody>(),
			ModContent.ItemType<HibiscusLegs>()
		];
		}
	}
}
