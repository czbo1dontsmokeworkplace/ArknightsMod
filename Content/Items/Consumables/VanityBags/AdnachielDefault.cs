using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Adnachiel;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class AdnachielDefault : ArknightsVanityBag
	{
		public override int Rarity => 3;
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<AdnachielHead>(),
			ModContent.ItemType<AdnachielBody>(),
			ModContent.ItemType<AdnachielLegs>()
		];
		}
	}
}
