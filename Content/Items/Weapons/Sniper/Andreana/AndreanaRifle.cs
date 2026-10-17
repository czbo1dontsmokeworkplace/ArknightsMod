using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class AndreanaRifle : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Andreana;
    protected override int PlaceholderItem => ItemID.OnyxBlaster;
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] EliteDamage => [392, 460, 536];
    public override void AddRecipes() => CreateRecipe().AddIngredient<AmbrielRifle>().AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 14).AddIngredient(ItemID.SoulofNight, 12).AddTile(TileID.MythrilAnvil).Register();
}
