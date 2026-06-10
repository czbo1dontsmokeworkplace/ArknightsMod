using System.Collections.Generic;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class KroosAlterDefault:ArknightsVanityBag
	{
		public override int Rarity => 4;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterHead>(),
			ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterBody>(),
			ModContent.ItemType<Armor.Vanity.Sniper.KroosAlter.KkdyAlterLegs>()
			];
		}
	}
}
