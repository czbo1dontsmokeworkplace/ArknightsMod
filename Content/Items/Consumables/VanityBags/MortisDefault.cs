using ArknightsMod.Content.Items.Armor.Vanity.Specialist.Mortis;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MortisDefault : ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<MortisHead>(),
			ModContent.ItemType<MortisBody>(),
			ModContent.ItemType<MortisLegs>()
		];
		}
	}
}
