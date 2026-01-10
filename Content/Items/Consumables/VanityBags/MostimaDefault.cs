using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MostimaDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Vanity.Caster.Mostima.MostimaHead>(),
			ModContent.ItemType<Armor.Vanity.Caster.Mostima.MostimaBody>(),
			ModContent.ItemType<Armor.Vanity.Caster.Mostima.MostimaLegs>()
			];
		}
	}
}
