using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class FartoothDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Sniper.Fartooth.FartoothHead>(),
			ModContent.ItemType<Armor.Sniper.Fartooth.FartoothBody>(),
			ModContent.ItemType<Armor.Sniper.Fartooth.FartoothLegs>()
		];
		}
	}
}
