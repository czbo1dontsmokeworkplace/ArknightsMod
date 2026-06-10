using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KaltsitDefault: ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitHead>(),
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitBody>(),
			ModContent.ItemType<Armor.Vanity.Medic.Kaltsit.KaltsitLegs>()
		];
		}
	}
}
