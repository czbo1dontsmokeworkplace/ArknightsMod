using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

public sealed class ArchettoCrossbow : BranchCrossbowWeapon
{
    public override CrossbowKind Kind => CrossbowKind.Archetto;
    protected override int PlaceholderItem => ItemID.ChlorophyteShotbow;
    protected override int Rarity => ItemRarityID.Lime;
    protected override int[] Damages => [35, 41, 48];
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Branches.GreyThroatCrossbow>()
        .AddIngredient(ItemID.ChlorophyteBar, 14).AddIngredient(ItemID.SoulofFright, 10)
        .AddIngredient(ItemID.Ectoplasm, 10).AddTile(TileID.MythrilAnvil).Register();
}
