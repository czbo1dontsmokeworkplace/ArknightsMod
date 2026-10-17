using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Content.Projectiles.Medic.Sussurro;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic.Sussurro;

// By T
/// <summary>困难模式初期的攻击医疗法杖：射击与治疗各自计时，不以敌人数量放大回血。</summary>
public sealed class SussurroStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [54, 62, 70];
    public override void SetStaticDefaults()
    {
        Item.staff[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        // 四连发：第 0、4、8、12 帧射击，27 帧开始下一轮。
        // 不能把 useAnimation 拉到治疗间隔，否则四发结束后法杖会长期停在举起状态。
        Item.useTime = 4;
        Item.useAnimation = 27;
        Item.useLimitPerAnimation = 4;
        Item.mana = 8;
        Item.noMelee = true;
        Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 1.5f;
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: 3);
        Item.shoot = ModContent.ProjectileType<SussurroNeedle>();
        Item.shootSpeed = 12f;
    }

    public override bool AltFunctionUse(Player player) => false;

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        Item.noUseGraphic = false;
        if (player.whoAmI != Main.myPlayer)
            return;
        if (ArknightsKeybinds.SkillActivatePressed(player))
            player.GetModPlayer<SussurroStaffPlayer>().TryActivate();
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<SussurroStaffPlayer>().TryActivate();
            return false;
        }
        return player.altFunctionUse != 2 && base.CanUseItem(player);
    }
    public override float UseSpeedMultiplier(Player player) => 1f;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer)
            return false;
        SussurroStaffPlayer treatment = player.GetModPlayer<SussurroStaffPlayer>();
        Vector2 direction = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
        // 枪口也沿方块碰撞移动，贴墙射击不能把激光直接生成到墙后。
        position = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(9f),
            direction * 36f, 18, 18, true, true);
        if (player.altFunctionUse == 2)
            return false;
        treatment.RegisterCast(false);

        direction = direction.RotatedByRandom(MathHelper.ToRadians(2f));
        bool compressed = treatment.NextCompressedShot();
        int projectileType = compressed ? ModContent.ProjectileType<SussurroCompressionRay>() : type;
        int shotDamage = compressed ? (int)MathF.Round(damage * 1.6f) : damage;
        int index = Projectile.NewProjectile(source, position, direction * 12f,
            projectileType, shotDamage, knockback, player.whoAmI, treatment.DeepTreatment ? 1f : 0f);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        SoundEngine.PlaySound((compressed ? SoundID.Item158 : SoundID.Item91)
            with { Volume = compressed ? .32f : .22f, Pitch = compressed ? .15f : .42f, MaxInstances = 3 }, position);
        return false;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient<global::ArknightsMod.Content.Items.Material.RMA7012>(1)
            .AddIngredient<global::ArknightsMod.Content.Items.Material.Device>(2).AddIngredient<global::ArknightsMod.Content.Items.Weapons.Medic.Warfarin.WarfarinStaff>()
            .AddIngredient(ItemID.HealingPotion, 100)
            .AddRecipeGroup(OperatorWeaponRecipeGroups.AnyVanillaTombstone, 2)
            .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>()).Register();
    }
}
