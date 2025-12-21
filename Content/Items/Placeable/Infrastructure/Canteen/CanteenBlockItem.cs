using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.Canteen;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using ArknightsMod.Content.Tiles.Infrastructure.Medical;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.Canteen
{
	public class CanteenBlockItem : ArknightsInfraBlock
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<CanteenBlockTile>());
			Item.value = Item.sellPrice(0, 0, 0, 30);
		}
	}
}