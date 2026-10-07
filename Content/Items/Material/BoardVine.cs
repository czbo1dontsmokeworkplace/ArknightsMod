using ArknightsMod.Common;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class BoardVine : RareCollectibleItem
	{
		public override int BaseOriginiumIngotValue => BoardVinePlayer.BaseValue;
		protected override int PlaceableTileType => ModContent.TileType<Tiles.Natural.BoardVine>();

		public override void UpdateInventory(Player player) {
			int otherCount = RareCollectibleInventoryHelper.CountOtherTypesInInventory(player, Item.type);
			Item.value = 8 + otherCount * 4;
		}
	}
}
