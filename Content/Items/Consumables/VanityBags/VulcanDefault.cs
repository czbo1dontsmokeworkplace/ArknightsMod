using ArknightsMod.Content.Items.Armor.Vanity.Defender.Vulcan;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class VulcanDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<VulcanHead>(),
				ModContent.ItemType<VulcanBody>(),
				ModContent.ItemType<VulcanLegs>()
			];
		}
	}
}
