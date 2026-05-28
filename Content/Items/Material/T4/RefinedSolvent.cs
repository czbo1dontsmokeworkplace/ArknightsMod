using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T4
{
	/// <summary>
	/// 精炼溶剂，稀有度：粉
	/// </summary>
	public class RefinedSolvent : ArknightsMaterial
	{
		public override int Rarity => 3;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<SemiSyntheticSolvent>(1)
				.AddIngredient<CompoundCuttingFluid>(1)
				.AddIngredient<CoagulatingGel>(1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedMechBossAny)
				.Register();
		}
	}
}
