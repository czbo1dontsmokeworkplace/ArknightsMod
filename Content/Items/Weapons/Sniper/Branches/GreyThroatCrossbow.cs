using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

public sealed class GreyThroatCrossbow : BranchCrossbowWeapon
{
    public override CrossbowKind Kind => CrossbowKind.GreyThroat;
    protected override int PlaceholderItem => ItemID.MythrilRepeater;
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] Damages => [44, 52, 60];
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter.KroosAlterCrossbow>()
        .AddIngredient(ItemID.SoulofFlight, 12).AddIngredient(ItemID.SoulofMight, 8).AddTile(TileID.MythrilAnvil).Register();
}
