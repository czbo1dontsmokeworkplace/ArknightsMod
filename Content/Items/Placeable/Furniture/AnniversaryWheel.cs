using ArknightsMod.Content.Tiles.Furniture;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Furniture
{
	public class AnniversaryWheel : ModItem
	{
		public override void SetStaticDefaults() {

			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<AnniversaryWheelTile>());
			Item.value = 150;
			Item.maxStack = 99;
			Item.width = 32;
			Item.height = 32;
		}
	}
}
