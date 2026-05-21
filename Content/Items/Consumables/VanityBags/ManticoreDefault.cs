using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ManticoreDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Specialist.Manticore.ManticoreHead>(),
			ModContent.ItemType<Armor.Specialist.Manticore.ManticoreBody>(),
			ModContent.ItemType<Armor.Specialist.Manticore.ManticoreLegs>()
			];
		}
	}
}
