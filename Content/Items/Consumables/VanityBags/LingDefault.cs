using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LingDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Supporter.Ling.LingHead>(),
			ModContent.ItemType<Armor.Supporter.Ling.LingBody>(),
			ModContent.ItemType<Armor.Supporter.Ling.LingLegs>()
		];
		}
	}
}
