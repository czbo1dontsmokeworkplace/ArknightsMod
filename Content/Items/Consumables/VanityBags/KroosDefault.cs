	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Kroos;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class KroosDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<KroosHead>(),
			ModContent.ItemType<KroosBody>(),
			ModContent.ItemType<KroosLegs>()
		};
		}
	}
}
