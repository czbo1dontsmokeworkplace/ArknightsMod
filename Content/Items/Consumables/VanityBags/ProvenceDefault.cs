using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Provence;
using System.Collections.Generic;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ProvenceDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<ProvenceHead>(),
			ModContent.ItemType<ProvenceBody>(),
			ModContent.ItemType<ProvenceLegs>()
		];
		}
	}
}
