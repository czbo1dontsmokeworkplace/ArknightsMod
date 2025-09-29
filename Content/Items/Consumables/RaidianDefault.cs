using ArknightsMod.Content.Items.Armor.Vanity.Guard;
using ArknightsMod.Content.Items.Armor.Vanity.Supporter;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class RaidianDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<RaidianHead>(),
			ModContent.ItemType<RaidianBody>(),
			ModContent.ItemType<RaidianLegs>()
		};
		}
	}
}
