using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Wisadel;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class WisdelDefault : ArknightsVanityBag
	{
		public override int Rarity => 6;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<WisadelHead>(),
			ModContent.ItemType<WisadelBody>(),
			ModContent.ItemType<WisadelLegs>()
		];
		}
	}
}
