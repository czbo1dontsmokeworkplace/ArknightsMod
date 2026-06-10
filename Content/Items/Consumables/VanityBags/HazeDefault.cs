using System.Collections.Generic;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	internal class HazeDefault:ArknightsVanityBag
	{
		public override int Rarity => 4;
		protected override List<int> GetItems() {
			return
			[
				ModContent.ItemType<Armor.Vanity.Caster.Haze.HazeHead>(),
			ModContent.ItemType<Armor.Vanity.Caster.Haze.HazeBody>(),
			ModContent.ItemType<Armor.Vanity.Caster.Haze.HazeLegs>()
			];
		}
	}
}
