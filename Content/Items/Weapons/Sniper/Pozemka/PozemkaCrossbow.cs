using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Pozemka;

public class PozemkaCrossbow : CrossbowWeaponBase
{
    public override CrossbowKind Kind => CrossbowKind.Pozemka;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults() {
        Item.damage = 175;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 88;
        Item.height = 44;
        Item.scale = 0.7f;

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true; //Channel so that you can held the weapon [Important]
        Item.knockBack = 5;
        Item.shootSpeed = 20f;
        Item.useAmmo = AmmoID.Arrow;
        Item.crit = 0; // The percent chance at hitting an enemy with a crit, plus the default amount of 4.
        Item.autoReuse = true;

        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(0, 0, 10, 0);

        SetCrossbowDefaults();
    }

    public override void AddRecipes() {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Schwarz.SchwarzBow>();
        recipe.AddIngredient(ItemID.FragmentSolar, 10);
        recipe.AddIngredient(ItemID.FragmentVortex, 10);
        recipe.AddIngredient(ItemID.FragmentNebula, 10);
        recipe.AddIngredient(ItemID.FragmentStardust, 10);
        recipe.AddTile(ModContent.TileType<FactoryTile>());
        recipe.Register();
    }
}
