using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Endfield.Striker.Yvonne;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class YvonneVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/Yvonne/Yvonne";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<YvonneHead>(),
				ModContent.ItemType<YvonneBody>(),
				ModContent.ItemType<YvonneLegs>(),
			];
		}
	}
}
