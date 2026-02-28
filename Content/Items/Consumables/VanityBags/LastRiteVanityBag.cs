using System.Collections.Generic;
using ArknightsMod.Content.Items.Armor.Vanity.Endfield.Striker.LastRite;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public class LastRiteVanityBag : ArknightsVanityBag
	{
		public override string Texture => "ArknightsMod/Content/Items/Armor/Vanity/Endfield/Striker/LastRite/LastRite_GIFFullArmor";

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.consumable = true;
			Item.maxStack = 9999;
			Item.rare = ItemRarityID.White;
			Item.width = 30;
			Item.height = 56;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.UseSound = SoundID.Item4;
		}

		public override bool CanRightClick()
		{
			return Item.stack == 1;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			foreach (int itemType in GetItems())
				player.QuickSpawnItem(player.GetSource_OpenItem(Type), itemType, 1);

			return true;
		}

		protected override List<int> GetItems()
		{
			return new List<int>
			{
				ModContent.ItemType<LastRiteHead>(),
				ModContent.ItemType<LastRiteBody>(),
				ModContent.ItemType<LastRiteLegs>(),
			};
		}
	}
}
