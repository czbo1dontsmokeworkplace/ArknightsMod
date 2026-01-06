using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Lava;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class LavaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<LavaHead>(),
			ModContent.ItemType<LavaBody>(),
			ModContent.ItemType<LavaLegs>()
		];
		}
	}
}
