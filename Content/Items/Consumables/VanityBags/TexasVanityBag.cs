using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Texas;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class TexasVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Vanguard/Texas/Texas";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Pink;
		}

		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<TexasHead>(),
				ModContent.ItemType<TexasBody>(),
				ModContent.ItemType<TexasLegs>(),
			];
		}
	}
}
