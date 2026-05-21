using System.Collections.Generic;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KroosAlterDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Sniper.KroosAlter.KkdyAlterHead>(),
			ModContent.ItemType<Armor.Sniper.KroosAlter.KkdyAlterBody>(),
			ModContent.ItemType<Armor.Sniper.KroosAlter.KkdyAlterLegs>()
			];
		}
	}
}
