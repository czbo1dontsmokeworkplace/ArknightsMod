using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class CivilightEternaDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Supporter.CivilightEterna.CivilightEternaHead>(),
			ModContent.ItemType<Armor.Supporter.CivilightEterna.CivilightEternaBody>(),
			ModContent.ItemType<Armor.Supporter.CivilightEterna.CivilightEternaLegs>()
			];
		}
	}
}
