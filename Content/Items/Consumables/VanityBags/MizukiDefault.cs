using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MizukiDefault : ArknightsVanityBag
	{

		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Specialist.Mizuki.MizukiHead>(),
			ModContent.ItemType<Armor.Specialist.Mizuki.MizukiBody>(),
			ModContent.ItemType<Armor.Specialist.Mizuki.MizukiLegs>()
			];
		}
	}
}
