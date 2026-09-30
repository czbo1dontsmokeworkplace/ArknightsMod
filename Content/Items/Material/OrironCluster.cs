using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class OrironCluster : ArknightsMaterial
	{
		// 蓝色（中级材料）：与固源岩组/糖组/聚酸酯组/酮凝集组等同档材料保持一致，
		// 明日方舟里异铁组同样是蓝色档。
		public override int Rarity => 2;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<Oriron>(4)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.Hardmode)
				.Register();
		}
	}
}
