using System.Collections.Generic;
using Terraria.ModLoader;


namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class BeagleDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Vanity.Defender.Beagle.BeagleHead>(),
			ModContent.ItemType<Armor.Vanity.Defender.Beagle.BeagleBody>(),
			ModContent.ItemType<Armor.Vanity.Defender.Beagle.BeagleLegs>()
		];
		}
	}
}
