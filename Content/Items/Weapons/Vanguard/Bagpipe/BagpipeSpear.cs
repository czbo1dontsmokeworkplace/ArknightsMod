using ArknightsMod.Content.Items.Weapons.Vanguard.WildMane;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.Bagpipe;

// Same registered identity and original art; old stab projectiles are no longer fired.
public class BagpipeSpear : VanguardSpearWeapon
{
    public override VanguardSpearKind Kind => VanguardSpearKind.Bagpipe;
    protected override int Rarity => ItemRarityID.Yellow;
    protected override int[] EliteDamage => [116, 137, 158];
    public override void AddRecipes() => CreateRecipe().AddIngredient<WildManeLance>()
        .AddIngredient(ItemID.BeetleHusk, 6).AddIngredient(ItemID.ChlorophyteBar, 12)
        .AddIngredient(ItemID.ExplosivePowder, 20).AddTile(TileID.MythrilAnvil).Register();
}
