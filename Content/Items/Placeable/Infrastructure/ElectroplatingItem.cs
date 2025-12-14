using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure
{
	public class ElectroplatingItem : ArknightsInfraBlock
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<ElectroplatingTile>());
			Item.value = Item.sellPrice(0, 0, 0, 30);
		}
	}
}