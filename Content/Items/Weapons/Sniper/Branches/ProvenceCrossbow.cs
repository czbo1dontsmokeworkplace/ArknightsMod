using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

public sealed class ProvenceCrossbow : BranchCrossbowWeapon
{
    public override CrossbowKind Kind => CrossbowKind.Provence;
    protected override int PlaceholderItem => ItemID.AdamantiteRepeater;
    protected override int Rarity => ItemRarityID.LightPurple;
    protected override int[] Damages => [393, 454, 526];
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Branches.BluePoisonCrossbow>()
        .AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.SoulofMight, 8)
        .AddIngredient(ItemID.Ectoplasm, 10).AddTile(TileID.MythrilAnvil).Register();
}
