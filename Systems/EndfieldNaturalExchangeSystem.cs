using System.Linq;
using ArknightsMod.Content.Items.Consumables;
using ArknightsMod.Content.Items.Material.ReclamAlgor;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Systems;

public sealed class EndfieldNaturalExchangeSystem : ModSystem
{
    public const string AnyFungus = "ArknightsMod:EndfieldFungus";
    public const string AnyLeaf = "ArknightsMod:EndfieldLeaf";
    public const string AnyStone = "ArknightsMod:EndfieldStone";

    public override void AddRecipeGroups()
    {
        Register(AnyFungus, "AnyFungus", 0);
        Register(AnyLeaf, "AnyLeaf", 6);
        Register(AnyStone, "AnyStone", 12);
    }

    private static void Register(string group, string text, int start) => RecipeGroup.RegisterGroup(group,
        new RecipeGroup(() => Language.GetTextValue($"Mods.ArknightsMod.EndfieldNatural.{text}"),
            Enumerable.Range(start, 6).Select(EndfieldNaturalCatalog.ItemType).ToArray()));

    public override void AddRecipes()
    {
        // 增加菜谱，保留原有生息演算食品的配方和食用效果。
        Recipe.Create(ModContent.ItemType<RAMeatchip>())
            .AddRecipeGroup(AnyFungus, 2).AddIngredient<RAMeat>()
            .AddTile(TileID.CookingPots).Register();
        Recipe.Create(ModContent.ItemType<RARicecrab>())
            .AddRecipeGroup(AnyFungus).AddRecipeGroup(AnyLeaf).AddIngredient<RiceGrain>().AddIngredient<CrabClaw>()
            .AddTile(TileID.CookingPots).Register();
        Recipe.Create(ModContent.ItemType<SeasonedRations>())
            .AddRecipeGroup(AnyLeaf, 2).AddIngredient<RiceGrain>().AddIngredient<RAWater>()
            .AddTile(TileID.CookingPots).Register();
        Recipe.Create(ModContent.ItemType<DehydratedSeasonedJerky>())
            .AddRecipeGroup(AnyLeaf).AddRecipeGroup(AnyFungus).AddIngredient<RALegmeat>()
            .AddTile(TileID.CookingPots).Register();

        // 探索、照明和建造耗材。储存箱按本轮用户要求跳过。
        Recipe.Create(ItemID.SpelunkerPotion, 2).AddRecipeGroup(AnyStone, 2).AddRecipeGroup(AnyLeaf)
            .AddIngredient(ItemID.BottledWater, 2).AddTile(TileID.Bottles).Register();
        Recipe.Create(ItemID.MiningPotion, 2).AddRecipeGroup(AnyStone, 2).AddRecipeGroup(AnyFungus)
            .AddIngredient(ItemID.BottledWater, 2).AddTile(TileID.Bottles).Register();
        Recipe.Create(ItemID.RecallPotion, 3).AddRecipeGroup(AnyLeaf, 2).AddIngredient(ItemID.BottledWater, 3)
            .AddTile(TileID.Bottles).Register();
        Recipe.Create(ItemID.Torch, 30).AddRecipeGroup(AnyFungus).AddRecipeGroup(RecipeGroupID.Wood, 5)
            .AddTile(TileID.WorkBenches).Register();
        Recipe.Create(ItemID.Rope, 50).AddRecipeGroup(AnyLeaf, 2)
            .AddTile(TileID.WorkBenches).Register();
        Recipe.Create(ItemID.Wire, 20).AddRecipeGroup(AnyStone, 2)
            .AddTile<FactoryTile>().Register();
    }
}
