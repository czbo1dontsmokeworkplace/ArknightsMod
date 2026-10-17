using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

public sealed class MelaniteCrossbow : BranchCrossbowWeapon
{
    public override CrossbowKind Kind => CrossbowKind.Melanite;
    protected override int PlaceholderItem => ItemID.HallowedRepeater;
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] Damages => [91, 105, 121];
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter.KroosAlterCrossbow>()
        .AddIngredient(ItemID.SoulofFright, 8).AddIngredient(ItemID.CrystalShard, 15)
        .AddTile(TileID.MythrilAnvil).Register();
}
