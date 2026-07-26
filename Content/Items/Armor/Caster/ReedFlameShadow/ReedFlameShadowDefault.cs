using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Caster.ReedFlameShadow;

namespace ArknightsMod.Content.Items.Armor.Caster.ReedFlameShadow
{
	public class ReedFlameShadowDefault : ArknightsVanityBag
	{
		public override int Rarity => 5;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<ReedFlameShadowHead>(),
				ModContent.ItemType<ReedFlameShadowBody>(),
				ModContent.ItemType<ReedFlameShadowLegs>()
			];
		}
	}
}
