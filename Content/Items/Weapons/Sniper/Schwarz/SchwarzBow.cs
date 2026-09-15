using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Schwarz;

public class SchwarzBow : CrossbowWeaponBase
{
    public override CrossbowKind Kind => CrossbowKind.Schwarz;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
    {
        Item.width = 62;
        Item.height = 32;
        Item.rare = ItemRarityID.Orange;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.value = Item.sellPrice(0, 40, 30, 0);
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 168;
        Item.knockBack = 10f;
        Item.noMelee = true;
        Item.shootSpeed = 20f;
        SetCrossbowDefaults();
    }

    public override void AddRecipes() {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter.KroosAlterCrossbow>();
        recipe.AddIngredient(ItemID.ChlorophyteBar, 15);
        recipe.AddIngredient(ItemID.SniperScope);
        recipe.AddTile(ModContent.TileType<FactoryTile>());
        recipe.Register();
    }
}
