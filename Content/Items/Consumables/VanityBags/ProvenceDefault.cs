using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Provence;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class ProvenceDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<ProvenceHead>(),
			ModContent.ItemType<ProvenceBody>(),
			ModContent.ItemType<ProvenceLegs>()
		};
		}
	}
}
