using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class HazeDefault:ArknightsVanityBag
	{
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Caster.Haze.HazeHead>(),
			ModContent.ItemType<Armor.Caster.Haze.HazeBody>(),
			ModContent.ItemType<Armor.Caster.Haze.HazeLegs>()
			];
		}
	}
}
