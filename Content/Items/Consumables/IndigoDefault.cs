using ArknightsMod.Content.Items.Armor.Vanity.Caster;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class IndigoDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<IndigoHead>(),
			ModContent.ItemType<IndigoBody>(),
			ModContent.ItemType<IndigoLegs>()
		};
		}
	}
}
