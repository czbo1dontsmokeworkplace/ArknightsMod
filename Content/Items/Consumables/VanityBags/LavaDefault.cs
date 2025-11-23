	using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Lava;
using ArknightsMod.Content.Items.Armor.Vanity.Caster.Amiya;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class LavaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<LavaHead>(),
			ModContent.ItemType<LavaBody>(),
			ModContent.ItemType<LavaLegs>()
		};
		}
	}
}
