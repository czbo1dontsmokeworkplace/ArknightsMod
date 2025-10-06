using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Amiya;

namespace ArknightsMod.Content.Items.Consumables
{
	public class AmiyaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<AmiyaHead>(),
			ModContent.ItemType<AmiyaBody>(),
			ModContent.ItemType<AmiyaLegs>()
		};
		}
	}
}
