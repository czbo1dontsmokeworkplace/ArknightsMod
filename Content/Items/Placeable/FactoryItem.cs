using ArknightsMod.Content.Items.Placeable;
using ArknightsMod.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable
{
	public class FactoryItem : ModItem
	{
		public override void SetStaticDefaults() {
			ItemID.Sets.CanGetPrefixes[Type] = false;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<FactoryTile>(), 0);
		}
	}
}
