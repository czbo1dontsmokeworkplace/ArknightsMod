using ArknightsMod.Content.Items.Armor.Vanity.Specialist.TexasAlter;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class TexalterDefault : ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<TexalterHead>(),
			ModContent.ItemType<TexalterBody>(),
			ModContent.ItemType<TexalterLegs>()
		];
		}
	}
}
