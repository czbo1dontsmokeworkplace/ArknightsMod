using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class PlasmaProjector : ModItem
{
    internal const int ManaInterval = 8;
    public override string LocalizationCategory => "Items.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.LaserRifle}";
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 46; Item.height = 28;
        Item.damage = 45; Item.DamageType = DamageClass.Magic;
        Item.mana = 4; Item.useTime = Item.useAnimation = ManaInterval;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.channel = true;
        Item.knockBack = 1; Item.rare = ItemRarityID.Lime;
        Item.value = Item.sellPrice(gold: 8); Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<PlasmaProjectorHoldout>();
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter,
                (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction),
                type, damage, knockback, player.whoAmI);
        return false;
    }
}
