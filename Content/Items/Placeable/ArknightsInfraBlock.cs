using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Content.Tiles.Infrastructure.HROffice;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Placeable
{
	public abstract class ArknightsInfraBlock : ModItem {
		public sealed override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe(50)
				.AddIngredient<CarbonBrick>()
				.AddTile<FactoryTile>()
				.Register();
		}
	}
}