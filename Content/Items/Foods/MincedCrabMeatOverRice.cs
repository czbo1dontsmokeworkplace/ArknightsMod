using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Foods
{
	/// <summary>
	/// 蟹肉碎拌饭 - Buff还没写
	/// </summary>
	public class MincedCrabMeatOverRice : ModItem
	{
		public override void SetDefaults()
		{
			Item.height = 20;
			Item.width = 32;
			Item.rare = ItemRarityID.Yellow;
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.UseSound = SoundID.Item2;
			Item.maxStack = 99999;
			Item.consumable = true;
			Item.useAnimation = 17;
			Item.useTime = 17;
		}
		public override bool CanUseItem(Player player) => true;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<RiceGrain>(), 1)
				.AddIngredient(ModContent.ItemType<CrabClaw>(), 2)
				.AddTile(TileID.CookingPots)
				.Register();
		}
	}
}
