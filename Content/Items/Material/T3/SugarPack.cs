using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T3
{
	/// <summary>
	/// 糖组，稀有度：蓝
	/// </summary>
	public class SugarPack : ArknightsMaterial
	{
		public override int Rarity => 2;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<Sugar>(4)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.Hardmode)
				.Register();
		}
	}
}
