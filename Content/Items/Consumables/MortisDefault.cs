using ArknightsMod.Content.Items.Armor.Vanity.Specialist.Mortis;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class MortisDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<MortisHead>(),
			ModContent.ItemType<MortisBody>(),
			ModContent.ItemType<MortisLegs>()
		};
		}
	}
}
