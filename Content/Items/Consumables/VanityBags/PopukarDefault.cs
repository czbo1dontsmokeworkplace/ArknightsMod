	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Popukar;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class PopukarDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<PopukarHead>(),
			ModContent.ItemType<PopukarBody>(),
			ModContent.ItemType<PopukarLegs>()
		};
		}
	}
}
