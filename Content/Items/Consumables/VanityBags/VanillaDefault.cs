using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Vanilla;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class VanillaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<VanillaHead>(),
			ModContent.ItemType<VanillaBody>(),
			ModContent.ItemType<VanillaLegs>()
		];
		}
	}
}
