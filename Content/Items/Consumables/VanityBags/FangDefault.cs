using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Fang;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class FangDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<FangHead>(),
			ModContent.ItemType<FangBody>(),
			ModContent.ItemType<FangLegs>()
		];
		}
	}
}
