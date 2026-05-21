using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class MostimaDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Caster.Mostima.MostimaHead>(),
			ModContent.ItemType<Armor.Caster.Mostima.MostimaBody>(),
			ModContent.ItemType<Armor.Caster.Mostima.MostimaLegs>()
			];
		}
	}
}
