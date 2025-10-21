using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.HROffice
{
	public class OfficeWallItem : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 100;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableWall(ModContent.WallType<OfficeWallTile>());
			Item.value = Item.sellPrice(0, 0, 0, 30);
		}
	}
}