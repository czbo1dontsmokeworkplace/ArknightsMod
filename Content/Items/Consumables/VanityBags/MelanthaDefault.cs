using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MelanthaDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<MelanthaHead>(),
			ModContent.ItemType<MelanthaBody>(),
			ModContent.ItemType<MelanthaLegs>()
		];
		}
	}
}
