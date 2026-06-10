using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ManticoreDefault:ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Vanity.Specialist.Manticore.ManticoreHead>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Manticore.ManticoreBody>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Manticore.ManticoreLegs>()
			];
		}
	}
}
