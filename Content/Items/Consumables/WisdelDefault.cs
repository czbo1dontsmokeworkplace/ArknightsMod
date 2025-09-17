using ArknightsMod.Content.Items.Armor.Vanity.Sniper;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class WisdelDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<WisdelHead>(),
			ModContent.ItemType<WisdelBody>(),
			ModContent.ItemType<WisdelLegs>()
		};
		}
	}
}
