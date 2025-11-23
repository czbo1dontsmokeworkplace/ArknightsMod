	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Fang;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class FangDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<FangHead>(),
			ModContent.ItemType<FangBody>(),
			ModContent.ItemType<FangLegs>()
		};
		}
	}
}
