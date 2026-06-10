using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Catapult;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class CatapultDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<CatapultHead>(),
			ModContent.ItemType<CatapultBody>(),
			ModContent.ItemType<CatapultLegs>()
		];
		}
	}
}
