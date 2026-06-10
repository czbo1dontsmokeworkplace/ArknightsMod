using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Popukar;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class PopukarDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<PopukarHead>(),
			ModContent.ItemType<PopukarBody>(),
			ModContent.ItemType<PopukarLegs>()
		];
		}
	}
}
