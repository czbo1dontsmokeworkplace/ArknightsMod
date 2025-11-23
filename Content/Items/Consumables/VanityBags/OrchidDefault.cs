	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Supporter.Orchid;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class OrchidDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<OrchidHead>(),
			ModContent.ItemType<OrchidBody>(),
			ModContent.ItemType<OrchidLegs>()
		};
		}
	}
}
