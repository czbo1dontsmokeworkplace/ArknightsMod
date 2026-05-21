using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class WarfarinDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Medic.Warfarin.WarfarinHead>(),
			ModContent.ItemType<Armor.Medic.Warfarin.WarfarinBody>(),
			ModContent.ItemType<Armor.Medic.Warfarin.WarfarinLegs>()
		];
		}
	}
}
