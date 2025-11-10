using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class SurtrDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Guard.Surtr.SurtrHead>(),
			ModContent.ItemType<Armor.Vanity.Guard.Surtr.SurtrBody>(),
			ModContent.ItemType<Armor.Vanity.Guard.Surtr.SurtrLegs>()
		};
		}
	
	}
}
