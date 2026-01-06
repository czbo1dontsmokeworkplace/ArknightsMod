using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Steward;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class StewardDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<StewardHead>(),
			ModContent.ItemType<StewardBody>(),
			ModContent.ItemType<StewardLegs>()
		];
		}
	}
}
