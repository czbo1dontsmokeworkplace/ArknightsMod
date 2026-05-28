using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T1
{
	/// <summary>
	/// 源岩，稀有度：白
	/// </summary>
	public class Orirock : ArknightsMaterial
	{
		public override int Rarity => 0;
		public override void SafeSetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.OrirockCubeTile>());
		}
		public override void AddRecipes() {

		}
	}
}