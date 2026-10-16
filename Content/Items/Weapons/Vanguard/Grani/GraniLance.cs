using ArknightsMod.Content.Items.Weapons.Guard.Hongdou;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Grani;

public sealed class GraniLance : VanguardSpearWeapon
{
    public override VanguardSpearKind Kind => VanguardSpearKind.Grani;
    // Placeholder: Fang's existing spear until Grani's folding lance art is supplied.
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Vanguard/Fang/FangSpear";
    protected override int Rarity => ItemRarityID.LightRed;
    protected override int[] EliteDamage => [48, 57, 67];
    public override void AddRecipes() => CreateRecipe().AddIngredient<HongdouLance>()
        .AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 14)
        .AddIngredient(ItemID.SoulofLight, 8).AddIngredient(ItemID.CrystalShard, 12)
        .AddTile(TileID.Anvils).Register();
}
