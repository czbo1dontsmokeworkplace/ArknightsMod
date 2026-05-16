using ArknightsMod.Content.Items.Armor.Vanity.Caster.Indigo;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class IndigoDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<IndigoHead>(),
			ModContent.ItemType<IndigoBody>(),
			ModContent.ItemType<IndigoLegs>()
		];
		}
	}
}
