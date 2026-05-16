using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Medic.Ansel;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class AnselDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<AnselHead>(),
			ModContent.ItemType<AnselBody>(),
			ModContent.ItemType<AnselLegs>()
		];
		}
	}
}
