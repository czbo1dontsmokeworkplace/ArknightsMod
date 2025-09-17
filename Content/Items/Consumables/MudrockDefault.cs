using ArknightsMod.Content.Items.Armor.Vanity.Defender;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class MudrockDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<MudrockHelmet>(),
			ModContent.ItemType<MudrockChestplate>(),
			ModContent.ItemType<MudrockGreaves>()
		};
		}
	}
}
