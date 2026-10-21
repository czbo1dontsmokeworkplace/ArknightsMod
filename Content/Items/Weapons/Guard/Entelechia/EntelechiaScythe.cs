using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Guard.Entelechia;

// Preserve the existing registered item and supplied sprite/inventory identity.
public sealed class EntelechiaScythe : ReaperWeapon
{
    public override ReaperKind Kind => ReaperKind.Entelechia;
    protected override int[] EliteDamage => [123, 147, 172];
    public override void AddRecipes() => CreateRecipe().AddIngredient<LaPlumaScythe>()
        .AddIngredient(ItemID.FragmentSolar, 14).AddIngredient(ItemID.FragmentVortex, 8).AddTile(TileID.LunarCraftingStation).Register();
}