using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public sealed class AmbrielRifle : ChargedOperatorWeapon
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Sniper/Ambriel/AmbrielRifle";
    public override ChargedOperatorKind Kind => ChargedOperatorKind.Ambriel;
    protected override int PlaceholderItem => ItemID.Musket;
    protected override int Rarity => ItemRarityID.Orange;
    protected override int[] EliteDamage => [270, 317, 372];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Musket).AddIngredient(ItemID.Bone, 35).AddIngredient(ItemID.IllegalGunParts).AddTile(TileID.Anvils).Register();
}
