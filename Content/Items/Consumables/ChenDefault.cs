using ArknightsMod.Content.Items.Armor.Vanity.Guard.Chen;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class ChenDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<ChenHead>(),
			ModContent.ItemType<ChenBody>(),
			ModContent.ItemType<ChenLegs>()
		};
		}
	}
}
