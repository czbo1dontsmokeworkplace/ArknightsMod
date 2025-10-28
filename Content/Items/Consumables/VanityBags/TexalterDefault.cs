using ArknightsMod.Content.Items.Armor.Vanity.Specialist.TexasAlter;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class TexalterDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<TexalterHead>(),
			ModContent.ItemType<TexalterBody>(),
			ModContent.ItemType<TexalterLegs>()
		};
		}
	}
}
