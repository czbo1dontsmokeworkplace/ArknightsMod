using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Content.Items.Placeable.Infrastructure
{
	public class FactoryItem : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100;
		}
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<FactoryTile>(), 0);
		}
		public override void AddRecipes() {
			Recipe recipe= CreateRecipe();

			recipe.AddIngredient(ModContent.ItemType<OrironShard>(), 4);

			recipe.AddRecipeGroup("IronBar", 4);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
