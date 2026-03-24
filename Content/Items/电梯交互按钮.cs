using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items
{
	public class 电梯交互按钮 : ModItem
	{
		public override string Texture => "ArknightsMod/Content/Images/Elevator/电梯交互按钮";

		public override void SetDefaults()
		{
			Item.width = 16;
			Item.height = 32;
			Item.maxStack = 99;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;

			Item.consumable = true;
			Item.value = Item.buyPrice(silver: 10);
			Item.rare = ItemRarityID.Blue;
			Item.createTile = ModContent.TileType<Tiles.电梯交互按钮Tile>();
		}
	}
}
