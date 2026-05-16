using ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MatoimaruDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<MatoimaruHead>(),
			ModContent.ItemType<MatoimaruBody>(),
			ModContent.ItemType<MatoimaruLegs>()
		];
		}
	}
}
