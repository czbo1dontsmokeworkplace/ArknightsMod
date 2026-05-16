using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LingDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingHead>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingBody>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingLegs>()
		];
		}
	}
}
