using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	public class MorniaDefault : ArknightsVanityBag
	{
		public override int Rarity => 4;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<MorniaHead>(),
				ModContent.ItemType<MorniaBody>(),
				ModContent.ItemType<MorniaLegs>()
			];
		}
	}
}
