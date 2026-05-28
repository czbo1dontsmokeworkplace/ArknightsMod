using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T4
{
	/// <summary>
	/// 改量装置，稀有度：粉
	/// </summary>
	public class OptimizedDevice : ArknightsMaterial
	{
		public override int Rarity => 3;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<IntegratedDevice>(1)
				.AddIngredient<OrirockCluster>(2)
				.AddIngredient<Grindstone>(1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedMechBossAny)
				.Register();
		}
	}
}
