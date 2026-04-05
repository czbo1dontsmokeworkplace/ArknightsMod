using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items
{
	public class 电梯 : ModItem
	{
		public override string Texture => "ArknightsMod/Content/Images/Elevator/电梯";

		public override void SetStaticDefaults()
		{
			// 名称与描述在本地化文件中配置
		}

		public override void SetDefaults()
		{
			Item.width = 16;
			Item.height = 16;
			Item.maxStack = 99;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;

			Item.consumable = true;
			Item.value = Item.buyPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.createTile = ModContent.TileType<Tiles.电梯Tile>();
		}
	}
}
