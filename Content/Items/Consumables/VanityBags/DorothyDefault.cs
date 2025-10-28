using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Fiammetta;
using ArknightsMod.Content.Items.Armor.Vanity.Specialist.Dorothy;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class DorothyDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<DorothyHead>(),
			ModContent.ItemType<DorothyBody>(),
			ModContent.ItemType<DorothyLegs>()
		};
		}
	}
}
