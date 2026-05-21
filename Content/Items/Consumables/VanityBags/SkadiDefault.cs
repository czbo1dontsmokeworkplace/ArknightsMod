using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class SkadiDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Guard.Skadi.SkadiHead>(),
			ModContent.ItemType<Armor.Guard.Skadi.SkadiBody>(),
			ModContent.ItemType<Armor.Guard.Skadi.SkadiLegs>()
			];
		}
	}
}
