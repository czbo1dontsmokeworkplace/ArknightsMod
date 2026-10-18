using ArknightsMod.Content.Items.Weapons.Charged;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Melantha;

// Keep the original item identity, texture and existing inventory copies.
[LegacyName("MelanthaSword")]
public sealed class MelanthasSword : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Melantha;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Melantha/MelanthasSword";
    protected override int PlaceholderItem => ItemID.SilverBroadsword;
    protected override int Rarity => ItemRarityID.Blue;
    protected override int[] EliteDamage => [24, 29];
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 8)
        .AddIngredient(ItemID.Wood, 15).AddTile(TileID.Anvils).Register();
}
