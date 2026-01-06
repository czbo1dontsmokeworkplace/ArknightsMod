using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MizukiDefault : ArknightsVanityBag
	{

		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiHead>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiBody>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiLegs>()
			];
		}
	}
}
