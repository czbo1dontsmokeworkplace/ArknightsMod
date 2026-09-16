using ArknightsMod.Content.Projectiles.Phalanx;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Phalanx;

public abstract class PhalanxStaff : ExpansionWeaponBase
{
    public abstract int Tier { get; }
    public override string Texture => "Terraria/Images/Item_" + new[] { ItemID.AmberStaff, ItemID.RubyStaff, ItemID.AmethystStaff }[Tier];
    public override void SetDefaults()
    {
        Item.width = 42; Item.height = 58;
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Magic;
        Item.mana = 8 + Tier * 3;
        Item.useTime = Item.useAnimation = PhalanxCycle.ChargeDuration;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = Item.noUseGraphic = Item.noMelee = Item.autoReuse = true;
        Item.knockBack = 4f;
        Item.rare = new[] { ItemRarityID.Orange, ItemRarityID.Pink, ItemRarityID.Red }[Tier];
        Item.value = Item.sellPrice(gold: 2 + Tier * 5);
        Item.shoot = ModContent.ProjectileType<PhalanxWave>();
    }
    // The always-present focus owns the continuous 90-frame charge, including while idle.
    // Keeping the normal use pipeline disabled also prevents double mana payments.
    public override bool CanUseItem(Player player) => false;
    public override bool AltFunctionUse(Player player) => false;

}

public sealed class BeeswaxStaff : PhalanxStaff
{
    public override int Tier => 0;
    protected override int[] EliteDamage => [42, 49, 57];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.AmberStaff)
        .AddIngredient(ItemID.Amber, 6).AddIngredient(ItemID.SandBlock, 80).AddIngredient(ItemID.Bone, 30)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}

public sealed class CarnelianStaff : PhalanxStaff
{
    public override int Tier => 1;
    protected override int[] EliteDamage => [84, 97, 112];
    public override void AddRecipes() => CreateRecipe().AddIngredient<BeeswaxStaff>()
        .AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.SoulofMight, 8).AddIngredient(ItemID.Ruby, 8)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}

public sealed class LinStaff : PhalanxStaff
{
    public override int Tier => 2;
    protected override int[] EliteDamage => [150, 175, 205];
    public override void AddRecipes() => CreateRecipe().AddIngredient<CarnelianStaff>()
        .AddIngredient(ItemID.FragmentNebula, 12).AddIngredient(ItemID.Glass, 40).AddIngredient(ItemID.CrystalShard, 16)
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
}
