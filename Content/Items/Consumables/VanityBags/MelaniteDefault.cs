using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MelaniteDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Sniper.Melanite.MelaniteHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Melanite.MelaniteBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Melanite.MelaniteLegs>()
		];
		}
	
	}
}
