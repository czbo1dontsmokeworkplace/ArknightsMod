using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class WarfarinDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinHead>(),
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinBody>(),
			ModContent.ItemType<Armor.Vanity.Medic.Warfarin.WarfarinLegs>()
		};
		}
	}
}
