using ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class MatoimaruDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<MatoimaruHead>(),
			ModContent.ItemType<MatoimaruBody>(),
			ModContent.ItemType<MatoimaruLegs>()
		};
		}
	}
}
