using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class RosmontisDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Sniper.Rosmontis.RosmontisHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Rosmontis.RosmontisBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Rosmontis.RosmontisLegs>()
		};
		}
	}
}
