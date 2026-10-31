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
        Item.damage = 76;
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
        // Both pre-hardmode crossbows feed the same next-stage weapon.
        foreach (int crossbow in new[] {
            ModContent.ItemType<global::ArknightsMod.Content.Items.Weapons.Sniper.Kroos.KroosCrossbow>(),
            ModContent.ItemType<global::ArknightsMod.Content.Items.Weapons.Sniper.Adnachiel.AdnachielCrossbow>() })
        {
            CreateRecipe()
                .AddIngredient<global::ArknightsMod.Content.Items.Material.OrironCluster>(1)
                .AddIngredient<global::ArknightsMod.Content.Items.Material.Polyketon>(2)
                .AddIngredient(crossbow)
                .AddRecipeGroup(global::ArknightsMod.Content.Items.Weapons.OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 8)
                .AddIngredient(ItemID.SoulofLight, 15)
                .AddIngredient(ItemID.CrystalShard, 5)
                .AddTile(ModContent.TileType<FactoryTile>())
                .Register();
        }
    }
}
