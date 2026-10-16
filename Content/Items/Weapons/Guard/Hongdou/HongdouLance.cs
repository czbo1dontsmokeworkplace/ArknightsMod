using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Items.Weapons.Vanguard.Fang;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Guard.Hongdou;

// Keep the existing internal name so saved Vigna weapons receive the rework.
public class HongdouLance : VanguardSpearWeapon
{
    public override VanguardSpearKind Kind => VanguardSpearKind.Vigna;
    protected override int Rarity => ItemRarityID.Green;
    protected override int[] EliteDamage => [24, 29, 34];
    public override void AddRecipes() => CreateRecipe().AddIngredient<FangSpear>()
        .AddRecipeGroup(OperatorWeaponRecipeGroups.GoldOrPlatinumBar, 8).AddIngredient(ItemID.Ruby, 2)
        .AddTile(TileID.Anvils).Register();
}
