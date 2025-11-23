	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Midnight;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MidnightDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<MidnightHead>(),
			ModContent.ItemType<MidnightBody>(),
			ModContent.ItemType<MidnightLegs>()
		};
		}
	}
}
