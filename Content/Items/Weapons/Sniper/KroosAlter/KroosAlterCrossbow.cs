using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using ArknightsMod.Content.Tiles.Infrastructure;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter;

public class KroosAlterCrossbow : CrossbowWeaponBase
{
    public override CrossbowKind Kind => CrossbowKind.KroosAlter;
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
    {
        Item.width = 52;
        Item.height = 32;
        Item.shootSpeed = 21f;
        Item.damage = 72;
        Item.knockBack = 3f;
        Item.DamageType = DamageClass.Ranged;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.rare = ItemRarityID.Green;
        Item.useAmmo = AmmoID.Arrow;
        Item.value = Item.sellPrice(0);
        Item.noMelee = true;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        SetCrossbowDefaults();
    }

    public override void AddRecipes() {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Kroos.KroosCrossbow>();
        recipe.AddRecipeGroup(global::ArknightsMod.Content.Items.Weapons.OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 8);
        recipe.AddIngredient(ItemID.SoulofLight, 15);
        recipe.AddIngredient(ItemID.CrystalShard, 5);
        recipe.AddTile(ModContent.TileType<FactoryTile>());
        recipe.Register();
    }
}
