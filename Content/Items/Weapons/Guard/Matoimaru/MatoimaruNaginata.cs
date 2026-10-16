using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class MatoimaruNaginata : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Matoimaru;
    protected override int PlaceholderItem => ItemID.Muramasa;
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] EliteDamage => [67, 79, 92];
    public override void AddRecipes() => CreateRecipe().AddIngredient<MelanthaSword>().AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 14).AddIngredient(ItemID.SoulofNight, 8).AddTile(TileID.MythrilAnvil).Register();
}
