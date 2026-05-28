using ArknightsMod.Content.Items.Material.T4;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T5
{
	/// <summary>
	/// 双极纳米片，稀有度：金
	/// </summary>
	public class BipolarNanoflake : ArknightsMaterial
	{
		public override int Rarity => 4;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<OptimizedDevice>(1)
				.AddIngredient<WhiteHorseKohl>(2)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedPlantera)
				.Register();
		}
	}
}
