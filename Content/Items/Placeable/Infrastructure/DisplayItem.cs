using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure
{
	public class DisplayItem : ArknightsInfraFurniture
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<DisplayTile>());
			Item.value = Item.sellPrice(0, 0, 10, 0);
		}
	}
}