using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper.Typhon;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class TyphonDefault : ArknightsVanityBag
	{
		public override string Texture =>
			"ArknightsMod/Content/Items/Armor/Vanity/Sniper/Typhon/TyphonDefault";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

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
