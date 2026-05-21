using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ExusiaiDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Sniper.Exusiai.ExusiaiHead>(),
			ModContent.ItemType<Armor.Sniper.Exusiai.ExusiaiBody>(),
			ModContent.ItemType<Armor.Sniper.Exusiai.ExusiaiLegs>()
			];
		}
	}
}
