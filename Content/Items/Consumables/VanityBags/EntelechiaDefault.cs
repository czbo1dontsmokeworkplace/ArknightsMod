using ArknightsMod.Content.Items.Armor.Vanity.Guard.Entelechia;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class EntelechiaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems()
		{
			return new List<int>
			{
				ModContent.ItemType<EntelechiaHead>(),
				ModContent.ItemType<EntelechiaBody>(),
				ModContent.ItemType<EntelechiaLegs>()
			};
		}
	}
}
