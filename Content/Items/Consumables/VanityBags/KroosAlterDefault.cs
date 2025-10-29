using System.Collections.Generic;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KroosAlterDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return new List<int>
			{
				ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterLegs>()
			};
		}
	}
}
