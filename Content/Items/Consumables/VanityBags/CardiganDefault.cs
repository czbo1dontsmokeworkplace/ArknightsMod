	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Defender.Cardigan;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class CardiganDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<CardiganHead>(),
			ModContent.ItemType<CardiganBody>(),
			ModContent.ItemType<CardiganLegs>()
		};
		}
	}
}
