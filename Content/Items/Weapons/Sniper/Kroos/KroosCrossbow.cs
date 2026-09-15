using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Kroos;

public class KroosCrossbow : CrossbowWeaponBase
{
    public override CrossbowKind Kind => CrossbowKind.Kroos;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults() {
        Item.damage = 19;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 120;
        Item.height = 60;

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true; //Channel so that you can held the weapon [Important]
        Item.knockBack = 2;
        Item.shootSpeed = 9f;
        Item.useAmmo = AmmoID.Arrow;
        Item.crit = 16; // The percent chance at hitting an enemy with a crit, plus the default amount of 4.
        Item.autoReuse = true;

        Item.rare = ItemRarityID.Blue;
        Item.value = Item.sellPrice(0, 0, 3, 20);

        SetCrossbowDefaults();
    }

    public override void AddRecipes() {
        Recipe recipe = CreateRecipe();
        recipe.AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaWoodenBow, 1);
        recipe.AddIngredient(ItemID.Bunny, 1);
        recipe.AddIngredient(ItemID.Silk, 2);
        recipe.AddTile(ModContent.TileType<FactoryTile>());
        recipe.Register();
    }
}
