using ArknightsMod.Content.Items.Armor.Vanity.Guard.Lappland;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LapplandDefault: ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<LapplandHead>(),
			ModContent.ItemType<LapplandBody>(),
			ModContent.ItemType<LapplandLegs>()
		];
		}
	}
}
