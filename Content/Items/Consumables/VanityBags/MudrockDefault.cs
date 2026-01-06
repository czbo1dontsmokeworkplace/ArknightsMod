using ArknightsMod.Content.Items.Armor.Vanity.Defender.Mudrock;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MudrockDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<MudrockHelmet>(),
			ModContent.ItemType<MudrockChestplate>(),
			ModContent.ItemType<MudrockGreaves>()
		];
		}
	}
}
