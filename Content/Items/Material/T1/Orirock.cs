using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T1
{
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