using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ExusiaiDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Vanity.Sniper.Exusiai.ExusiaiHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Exusiai.ExusiaiBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Exusiai.ExusiaiLegs>()
			];
		}
	}
}
