using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class NearlRadiantSword : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.NearlRadiant;
    protected override int PlaceholderItem => ItemID.DayBreak;
    protected override int Rarity => ItemRarityID.Red;
    protected override int[] EliteDamage => [168, 198, 232];
    public override void AddRecipes() => CreateRecipe().AddIngredient<FrankaRapier>().AddIngredient(ItemID.FragmentSolar, 14).AddIngredient(ItemID.HallowedBar, 16).AddTile(TileID.LunarCraftingStation).Register();
}
