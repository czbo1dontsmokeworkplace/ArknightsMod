using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class FartoothGreatbow : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Fartooth;
    protected override int PlaceholderItem => ItemID.Phantasm;
    protected override int Rarity => ItemRarityID.Red;
    protected override int[] EliteDamage => [204, 240, 280];
    public override void AddRecipes() => CreateRecipe().AddIngredient<FirewatchRifle>().AddIngredient(ItemID.FragmentVortex, 14).AddIngredient(ItemID.ShroomiteBar, 12).AddTile(TileID.LunarCraftingStation).Register();
}
