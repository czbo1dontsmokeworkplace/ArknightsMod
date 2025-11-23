	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Plume;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class PlumeDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<PlumeHead>(),
			ModContent.ItemType<PlumeBody>(),
			ModContent.ItemType<PlumeLegs>()
		};
		}
	}
}
