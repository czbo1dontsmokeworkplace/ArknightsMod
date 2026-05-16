using ArknightsMod.Content.Items.Armor.Vanity.Guard.Chen;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class ChenDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<ChenHead>(),
			ModContent.ItemType<ChenBody>(),
			ModContent.ItemType<ChenLegs>()
		];
		}
	}
}
