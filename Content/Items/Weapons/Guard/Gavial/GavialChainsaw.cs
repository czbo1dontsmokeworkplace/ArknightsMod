using ArknightsMod.Content.Items.Weapons.Guard.Blaze;
using ArknightsMod.Content.Projectiles.Guard.Gavial;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Gavial;

// By T
public sealed class GavialChainsaw : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [GavialBalance.Damage, GavialBalance.Damage, GavialBalance.Damage];
    public override string Texture => "Terraria/Images/Item_" + ItemID.ButchersChainsaw;

    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Melee;
        Item.width = 60;
        Item.height = 32;
        Item.useTime = Item.useAnimation = 10;
        Item.knockBack = 7f;
        Item.crit = 4;
        Item.value = Item.sellPrice(gold: 8);
        Item.rare = ItemRarityID.Yellow;
        Item.autoReuse = Item.channel = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true;
        Item.UseSound = GavialVisuals.MotorSound;
        Item.shoot = ModContent.ProjectileType<GavialChainsawHoldout>();
        Item.shootSpeed = 1f;
    }

    public override bool AltFunctionUse(Player player) => false;

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<GavialChainsawPlayer>().TryActivate();
            return false;
        }
        return player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter,
                velocity.SafeNormalize(new Vector2(player.direction, 0f)), type, damage, knockback,
                player.whoAmI, ai0: player.selectedItem,
                ai1: player.GetModPlayer<GavialChainsawPlayer>().ActiveMode);
        return false;
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        damage *= GavialBalance.DamageMultiplier(player.GetModPlayer<GavialChainsawPlayer>().ActiveMode);
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<BlazeGreatsword>()
        .AddIngredient(ItemID.ButchersChainsaw)
        .AddIngredient(ItemID.ChlorophyteBar, 18)
        .AddIngredient(ItemID.BeetleHusk, 8)
        .AddTile<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()
        .AddCondition(Condition.DownedGolem)
        .Register();
}
