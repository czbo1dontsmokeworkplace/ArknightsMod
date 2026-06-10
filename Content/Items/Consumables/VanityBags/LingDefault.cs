using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LingDefault:ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingHead>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingBody>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingLegs>()
		];
		}
	}
}
