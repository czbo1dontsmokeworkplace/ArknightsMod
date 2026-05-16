using System.Collections.Generic;
using Terraria.ModLoader;


namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class BeagleDefault : ArknightsVanityBag
	{
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
