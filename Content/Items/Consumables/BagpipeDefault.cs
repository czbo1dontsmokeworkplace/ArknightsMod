using ArknightsMod.Content.Items.Armor.Vanity.Vanguard;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class BagpipeDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<BagpipeHead>(),
			ModContent.ItemType<BagpipeBody>(),
			ModContent.ItemType<BagpipeLegs>()
		};
		}
	}
}
