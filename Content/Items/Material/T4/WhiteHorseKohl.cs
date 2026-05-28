using ArknightsMod.Content.Items.Material.T3;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material.T4
{
	/// <summary>
	/// 白马醇，稀有度：粉
	/// </summary>
	public class WhiteHorseKohl : ArknightsMaterial
	{
		public override int Rarity => 3;
		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<LoxicKohl>(1)
				.AddIngredient<SugarPack>(1)
				.AddIngredient<RMA7012>(1)
				.AddTile(ModContent.TileType<FactoryTile>())
				.AddCondition(Condition.DownedMechBossAny)
				.Register();
		}
	}
}
