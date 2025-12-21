using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure
{
	public class BridgeStructureWallItem : ArknightsInfraBlock
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableWall(ModContent.WallType<BridgeStructureWall>());
			Item.value = Item.sellPrice(0, 0, 0, 30);
		}
	}
}