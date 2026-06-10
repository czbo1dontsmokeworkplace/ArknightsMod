using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MizukiDefault : ArknightsVanityBag
	{

		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiHead>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiBody>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiLegs>()
			];
		}
	}
}
