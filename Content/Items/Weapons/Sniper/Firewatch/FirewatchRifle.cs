using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class FirewatchRifle : ChargedOperatorWeapon
{
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Firewatch;
    protected override int PlaceholderItem => ItemID.SniperRifle;
    protected override int Rarity => ItemRarityID.Lime;
    protected override int[] EliteDamage => [148, 174, 204];
    public override void AddRecipes() => CreateRecipe().AddIngredient<AndreanaRifle>().AddIngredient(ItemID.ChlorophyteBar, 18).AddIngredient(ItemID.Ectoplasm, 8).AddTile(TileID.MythrilAnvil).Register();
}
