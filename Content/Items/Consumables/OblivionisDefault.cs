using ArknightsMod.Content.Items.Armor.Vanity.Guard.Oblivionis;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class OblivionisDefault : ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<OblivionisHead>(),
			ModContent.ItemType<OblivionisBody>(),
			ModContent.ItemType<OblivionisLegs>()
		};
		}
	}
}
