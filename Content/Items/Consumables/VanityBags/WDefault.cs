using ArknightsMod.Content.Items.Armor.Vanity.Sniper.W;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class WDefault : ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<WHead>(),
			ModContent.ItemType<WBody>(),
			ModContent.ItemType<WLegs>()
		];
		}
	}
}
