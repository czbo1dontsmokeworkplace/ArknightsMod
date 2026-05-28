using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T4
{
	/// <summary>
	/// 聚酸酯块，稀有度：粉
	/// </summary>
	public class PolyesterLump : ArknightsMaterial
	{
		public override int Rarity => 3;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<PolyesterPack>(2)
				.AddIngredient<Aketon>(1)
				.AddIngredient<LoxicKohl>(1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedMechBossAny)
				.Register();
		}
	}
}
