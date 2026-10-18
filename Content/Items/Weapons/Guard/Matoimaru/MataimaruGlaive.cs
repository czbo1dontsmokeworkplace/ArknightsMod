using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Items.Weapons.Guard.Melantha;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Matoimaru;

[LegacyName("MatoimaruNaginata")]
public sealed class MataimaruGlaive : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Matoimaru;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Matoimaru/MataimaruGlaive";
    protected override int PlaceholderItem => ItemID.Muramasa;
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] EliteDamage => [67, 79, 92];
    public override void AddRecipes() => CreateRecipe().AddIngredient<MelanthasSword>()
        .AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 14).AddIngredient(ItemID.SoulofNight, 8)
        .AddTile(TileID.MythrilAnvil).Register();
}
