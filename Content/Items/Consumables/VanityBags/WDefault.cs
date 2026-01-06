using ArknightsMod.Content.Items.Armor.Vanity.Sniper.W;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class WDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<WHead>(),
			ModContent.ItemType<WBody>(),
			ModContent.ItemType<WLegs>()
		];
		}
	}
}
