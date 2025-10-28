using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class CivilightEternaDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
				ModContent.ItemType<Armor.Vanity.Supporter.CivilightEterna.CivilightEternaHead>(),
			ModContent.ItemType<Armor.Vanity.Supporter.CivilightEterna.CivilightEternaBody>(),
			ModContent.ItemType<Armor.Vanity.Supporter.CivilightEterna.CivilightEternaLegs>()
			};
		}
	}
}
