using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class FartoothDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Sniper.Fartooth.FartoothHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Fartooth.FartoothBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.Fartooth.FartoothLegs>()
		};
		}
	}
}
