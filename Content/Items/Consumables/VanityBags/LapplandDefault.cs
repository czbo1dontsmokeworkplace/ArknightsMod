using ArknightsMod.Content.Items.Armor.Vanity.Guard.Lappland;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LapplandDefault: ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<LapplandHead>(),
			ModContent.ItemType<LapplandBody>(),
			ModContent.ItemType<LapplandLegs>()
		};
		}
	}
}
