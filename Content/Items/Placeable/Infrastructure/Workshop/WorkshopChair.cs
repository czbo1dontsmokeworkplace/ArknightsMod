using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using ArknightsMod.Content.Tiles.Infrastructure.TrainingRoom;
using ArknightsMod.Content.Tiles.Infrastructure.Workshop;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure.Workshop
{
	public class WorkshopChair : ArknightsInfraFurniture
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<WorkshopChairTile>());
			Item.value = Item.sellPrice(0, 0, 10, 0);
		}
	}
}