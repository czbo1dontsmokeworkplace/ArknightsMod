using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class MelanthaSword : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Melantha;
    protected override int PlaceholderItem => ItemID.SilverBroadsword;
    protected override int Rarity => ItemRarityID.Blue;
    protected override int[] EliteDamage => [24, 29];
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 8).AddIngredient(ItemID.Wood, 15).AddTile(TileID.Anvils).Register();
}
