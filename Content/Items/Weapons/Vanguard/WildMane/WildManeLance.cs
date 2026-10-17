using ArknightsMod.Content.Items.Weapons.Vanguard.Grani;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.WildMane;

public sealed class WildManeLance : VanguardSpearWeapon
{
    public override VanguardSpearKind Kind => VanguardSpearKind.WildMane;
    // Placeholder: reuse the mod's existing spear art until dedicated Wild Mane lance art is supplied.
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Hongdou/HongdouLance";
    protected override int Rarity => ItemRarityID.Pink;
    protected override int[] EliteDamage => [77, 91, 106];
    public override void AddRecipes() => CreateRecipe().AddIngredient<GraniLance>()
        .AddIngredient(ItemID.HallowedBar, 16).AddIngredient(ItemID.SoulofMight, 8)
        .AddIngredient(ItemID.SoulofSight, 8).AddIngredient(ItemID.SoulofFright, 8)
        .AddTile(TileID.MythrilAnvil).Register();
}
