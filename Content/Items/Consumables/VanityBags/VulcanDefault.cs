using ArknightsMod.Content.Items.Armor.Vanity.Defender.Vulcan;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class VulcanDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems()
		{
			return new List<int>
			{
				ModContent.ItemType<VulcanHead>(),
				ModContent.ItemType<VulcanBody>(),
				ModContent.ItemType<VulcanLegs>()
			};
		}
	}
}
