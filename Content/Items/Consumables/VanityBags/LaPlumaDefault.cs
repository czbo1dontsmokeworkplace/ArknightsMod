using System.Collections.Generic;
using Terraria.ModLoader;


namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class LaPlumaDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
			ModContent.ItemType<Armor.Guard.LaPluma.LaPlumaHead>(),
			ModContent.ItemType<Armor.Guard.LaPluma.LaPlumaBody>(),
			ModContent.ItemType<Armor.Guard.LaPluma.LaPlumaLegs>()
		];
		}
	}
}
