using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KaltsitDefault: ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Medic.Kaltsit.KaltsitHead>(),
			ModContent.ItemType<Armor.Medic.Kaltsit.KaltsitBody>(),
			ModContent.ItemType<Armor.Medic.Kaltsit.KaltsitLegs>()
		];
		}
	}
}
