using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class UtageDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<Armor.Vanity.Guard.Utage.UtageHead>(),
			ModContent.ItemType<Armor.Vanity.Guard.Utage.UtageBody>(),
			ModContent.ItemType<Armor.Vanity.Guard.Utage.UtageLegs>()
		};
		}
	}
}
