	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Steward;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class StewardDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<StewardHead>(),
			ModContent.ItemType<StewardBody>(),
			ModContent.ItemType<StewardLegs>()
		};
		}
	}
}
