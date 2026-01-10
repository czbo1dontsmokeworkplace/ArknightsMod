using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Defender.Spot;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class SpotDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<SpotHead>(),
			ModContent.ItemType<SpotBody>(),
			ModContent.ItemType<SpotLegs>()
		];
		}
	}
}
