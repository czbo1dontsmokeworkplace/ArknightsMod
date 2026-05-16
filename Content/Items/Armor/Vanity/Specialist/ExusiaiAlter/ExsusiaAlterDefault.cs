using ArknightsMod.Content.Items.Armor.Vanity.Specialist.ExusiaiAlter;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class ExusiaiAlterDefault : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Specialist/ExusiaiAlter/ExsusiaAlterDefault";

		protected override List<int> GetItems() {
			return new List<int>
			{
			ModContent.ItemType<ExusiaiAlterHead>(),
			ModContent.ItemType<ExusiaiAlterBody>(),
			ModContent.ItemType<ExusiaiAlterLegs>()
		};
		}
	}
}