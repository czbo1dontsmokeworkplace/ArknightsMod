using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LingDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingHead>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingBody>(),
			ModContent.ItemType<Armor.Vanity.Supporter.Ling.LingLegs>()
		};
		}
	}
}
