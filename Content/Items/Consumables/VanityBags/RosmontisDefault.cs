using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class RosmontisDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Sniper.Rosmontis.RosmontisHead>(),
			ModContent.ItemType<Armor.Sniper.Rosmontis.RosmontisBody>(),
			ModContent.ItemType<Armor.Sniper.Rosmontis.RosmontisLegs>()
		];
		}
	}
}
