using ArknightsMod.Content.Items.Armor.Vanity.Specialist.Dorothy;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class DorothyDefault:ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<DorothyHead>(),
			ModContent.ItemType<DorothyBody>(),
			ModContent.ItemType<DorothyLegs>()
		];
		}
	}
}
