using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	internal class CivilightEternaDefault:ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<CivilightEternaHelmet>(),
				ModContent.ItemType<CivilightEternaChestplate>(),
				ModContent.ItemType<CivilightEternaGreaves>()
			];
		}
	}
}
