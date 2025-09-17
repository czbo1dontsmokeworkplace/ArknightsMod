using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class AmiyaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Caster.AmiyaHead>(),
			ModContent.ItemType<Armor.Vanity.Caster.AmiyaBody>(),
			ModContent.ItemType<Armor.Vanity.Caster.AmiyaLegs>()
		};
		}
	}
}
