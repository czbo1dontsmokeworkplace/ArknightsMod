using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T3
{
	/// <summary>
	/// 固源岩组，稀有度：蓝
	/// </summary>
	public class OrirockCluster : ArknightsMaterial
	{
		public override int Rarity => 2;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<OrirockCube>(5)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.Hardmode)
				.Register();
		}
	}
}
