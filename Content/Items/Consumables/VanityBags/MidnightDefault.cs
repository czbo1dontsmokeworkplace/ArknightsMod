using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Guard.Midnight;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MidnightDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<MidnightHead>(),
			ModContent.ItemType<MidnightBody>(),
			ModContent.ItemType<MidnightLegs>()
		];
		}
	}
}
