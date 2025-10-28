using ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MelanthaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<MelanthaHead>(),
			ModContent.ItemType<MelanthaBody>(),
			ModContent.ItemType<MelanthaLegs>()
		};
		}
	}
}
