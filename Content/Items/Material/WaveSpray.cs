using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class WaveSpray : RareCollectibleItem
	{
		public override int BaseOriginiumIngotValue => WaveSprayPlayer.BaseValue;
		protected override int PlaceableTileType => ModContent.TileType<Tiles.Natural.WaveSpray>();

		public override void UpdateInventory(Player player) {
			var wavePlayer = player.GetModPlayer<WaveSprayPlayer>();
			wavePlayer.HasWaveSpray = true;
			Item.value = wavePlayer.Value;
		}
	}
}
