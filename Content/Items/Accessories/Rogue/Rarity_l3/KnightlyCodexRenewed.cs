using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.UI.Chat;
using System.Linq;
using System.Collections.Generic;
using Terraria.GameContent;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class KnightlyCodexRenewed : ModItem
    {
		public override void SetStaticDefaults() {
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(12, 0, 0, 0);
			Item.rare = ItemRarityID.Cyan;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {

		}


	}
}