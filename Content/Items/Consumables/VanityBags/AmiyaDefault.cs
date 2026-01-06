using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Amiya;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class AmiyaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<AmiyaHead>(),
			ModContent.ItemType<AmiyaBody>(),
			ModContent.ItemType<AmiyaLegs>()
		];
		}
	}
}
