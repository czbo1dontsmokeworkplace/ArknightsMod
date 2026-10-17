using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Branches;

// Existing Archetto items load into the replacement rapid-fire branch item.
[LegacyName("ArchettoCrossbow")]
public sealed class BluePoisonCrossbow : BranchCrossbowWeapon
{
    public override CrossbowKind Kind => CrossbowKind.BluePoison;
    protected override int PlaceholderItem => ItemID.ChlorophyteShotbow;
    protected override int Rarity => ItemRarityID.Lime;
    protected override int[] Damages => [40, 47, 55];
    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Branches.GreyThroatCrossbow>()
        .AddIngredient(ItemID.ChlorophyteBar, 14).AddIngredient(ItemID.SoulofFright, 10)
        .AddIngredient(ItemID.Ectoplasm, 10).AddTile(TileID.MythrilAnvil).Register();
}
