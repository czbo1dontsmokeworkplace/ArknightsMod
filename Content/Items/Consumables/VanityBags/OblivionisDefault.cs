using ArknightsMod.Content.Items.Armor.Vanity.Guard.Oblivionis;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class OblivionisDefault : ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<OblivionisHead>(),
			ModContent.ItemType<OblivionisBody>(),
			ModContent.ItemType<OblivionisLegs>()
		];
		}
	}
}
