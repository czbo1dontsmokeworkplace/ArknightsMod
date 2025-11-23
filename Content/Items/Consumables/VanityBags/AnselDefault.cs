	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Medic.Ansel;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class AnselDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<AnselHead>(),
			ModContent.ItemType<AnselBody>(),
			ModContent.ItemType<AnselLegs>()
		};
		}
	}
}
