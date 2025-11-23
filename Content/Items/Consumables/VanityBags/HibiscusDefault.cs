	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Medic.Hibiscus;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class HibiscusDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<HibiscusHead>(),
			ModContent.ItemType<HibiscusBody>(),
			ModContent.ItemType<HibiscusLegs>()
		};
		}
	}
}
