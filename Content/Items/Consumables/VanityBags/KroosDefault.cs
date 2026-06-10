using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Kroos;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class KroosDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<KroosHead>(),
			ModContent.ItemType<KroosBody>(),
			ModContent.ItemType<KroosLegs>()
		];
		}
	}
}
