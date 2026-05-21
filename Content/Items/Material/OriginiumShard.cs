using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class OriginiumShard : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100; 
		}

		public override void SetDefaults() {
			Item.width = 20; 
			Item.height = 20;
			Item.rare = ItemRarityID.Quest;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(0, 0, 2, 50); 
		}
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient<OrirockCube>(2);
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
			recipe = CreateRecipe();
			recipe.AddIngredient<Device>();
			recipe.AddTile(ModContent.TileType<FactoryTile>());
			recipe.Register();
		}
	}
}
