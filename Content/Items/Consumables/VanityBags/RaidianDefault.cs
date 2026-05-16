using ArknightsMod.Content.Items.Armor.Vanity.Supporter.Radian;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class RaidianDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<RaidianHead>(),
			ModContent.ItemType<RaidianBody>(),
			ModContent.ItemType<RaidianLegs>()
		];
		}
	}
}
