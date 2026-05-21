using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class SurtrDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Guard.Surtr.SurtrHead>(),
			ModContent.ItemType<Armor.Guard.Surtr.SurtrBody>(),
			ModContent.ItemType<Armor.Guard.Surtr.SurtrLegs>()
		];
		}
	
	}
}
