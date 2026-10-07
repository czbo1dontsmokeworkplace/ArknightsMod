using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	// 背包里只要有它就持续生效，具体的估价升降/损坏判定都在 FrostCrystalTreePlayer 里。
	public class FrostCrystalTree : RareCollectibleItem
	{
		public override int BaseOriginiumIngotValue => FrostCrystalTreePlayer.BaseValue;
		protected override int PlaceableTileType => ModContent.TileType<Tiles.Natural.FrostCrystalTree>();

		public override void UpdateInventory(Player player) {
			var frostPlayer = player.GetModPlayer<FrostCrystalTreePlayer>();
			frostPlayer.HasFrostCrystalTree = true;
			Item.value = frostPlayer.Value;
		}
	}
}
