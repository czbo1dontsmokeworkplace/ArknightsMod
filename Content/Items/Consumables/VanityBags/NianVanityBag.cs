using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Defender.Nian;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class NianVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Defender/Nian/NianVanityBag";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.rare = ItemRarityID.Red;
		}

		protected override List<int> GetItems()
		{
			return
			[
				ModContent.ItemType<NianHead>(),
				ModContent.ItemType<NianBody>(),
				ModContent.ItemType<NianLegs>(),
			];
		}
	}
}
