using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Guard.Ulpianus;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class UlpianusVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Guard/Ulpianus/UlpianusVanityBag";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<UlpianusHead>(),
				ModContent.ItemType<UlpianusBody>(),
				ModContent.ItemType<UlpianusLegs>(),
			];
		}
	}
}
