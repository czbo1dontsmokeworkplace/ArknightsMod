using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T4
{
	/// <summary>
	/// 提纯源岩，稀有度：粉
	/// </summary>
	public class OrirockConcentration : ArknightsMaterial
	{
		public override int Rarity => 3;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<OrirockCluster>(4)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedMechBossAny)
				.Register();
		}
	}
}
