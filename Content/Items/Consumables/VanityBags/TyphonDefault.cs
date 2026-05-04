using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Typhon;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class TyphonDefault : ArknightsVanityBag
	{
		public override string Texture =>
			"ArknightsMod/Content/Items/Armor/Vanity/Sniper/Typhon/TyphonDefault";

		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<TyphonHead>(),
				ModContent.ItemType<TyphonBody>(),
				ModContent.ItemType<TyphonLegs>()
			];
		}
	}
}
