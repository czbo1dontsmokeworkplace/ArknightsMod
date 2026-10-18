using ArknightsMod.Content.Items.Weapons.Guard.Matoimaru;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class FrankaRapier : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Franka;
    protected override int PlaceholderItem => ItemID.Cutlass;
    protected override int Rarity => ItemRarityID.Lime;
    protected override int[] EliteDamage => [112, 132, 154];
    public override void AddRecipes() => CreateRecipe().AddIngredient<MataimaruGlaive>().AddIngredient(ItemID.ChlorophyteBar, 16).AddIngredient(ItemID.HellstoneBar, 12).AddIngredient(ItemID.Ectoplasm, 6).AddTile(TileID.MythrilAnvil).Register();
}
