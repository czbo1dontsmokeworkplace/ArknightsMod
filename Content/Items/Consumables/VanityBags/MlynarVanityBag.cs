using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Mlynar;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class MlynarVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Guard/Mlynar/Mlynar";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

		public override ObtainTypes ObtainType => ObtainTypes.Limited_Celebration;
		public override int Rarity => 6;
		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<MlynarHead>(),
				ModContent.ItemType<MlynarBody>(),
				ModContent.ItemType<MlynarLegs>(),
			];
		}
	}
}
