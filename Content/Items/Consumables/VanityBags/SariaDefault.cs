using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class SariaDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Defender.Saria.SariaHead>(),
			ModContent.ItemType<Armor.Vanity.Defender.Saria.SariaBody>(),
			ModContent.ItemType<Armor.Vanity.Defender.Saria.SariaLegs>()
		};
		}
	}
}
