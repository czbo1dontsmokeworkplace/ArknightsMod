using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class Orundum : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 10;
		}
		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 20;
			Item.rare = ItemRarityID.Quest;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(0, 0, 1, 50);
		}
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe(10);
			recipe.AddIngredient<OriginiumShard>();
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
	}
}
