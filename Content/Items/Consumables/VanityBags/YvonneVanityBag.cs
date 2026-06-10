using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Endfield.Striker.Yvonne;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class YvonneVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Endfield/Striker/Yvonne/YvonneDefault";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

		public override ObtainTypes ObtainType => ObtainTypes.EndfieldDefault;
		public override int Rarity => 6;
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
