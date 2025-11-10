using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MizukiDefault : ArknightsVanityBag
	{

		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiHead>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiBody>(),
			ModContent.ItemType<Armor.Vanity.Specialist.Mizuki.MizukiLegs>()
			};
		}
	}
}
