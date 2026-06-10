using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class WarfarinDefault:ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinHead>(),
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinBody>(),
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinLegs>()
		];
		}
	}
}
