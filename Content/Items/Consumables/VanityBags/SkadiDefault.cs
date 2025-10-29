using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class SkadiDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Guard.Skadi.SkadiHead>(),
			ModContent.ItemType<Armor.Vanity.Guard.Skadi.SkadiBody>(),
			ModContent.ItemType<Armor.Vanity.Guard.Skadi.SkadiLegs>()
			};
		}
	}
}
