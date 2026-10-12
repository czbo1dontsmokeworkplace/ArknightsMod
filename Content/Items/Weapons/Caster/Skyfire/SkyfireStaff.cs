using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Caster.Leonhardt;
using ArknightsMod.Content.Projectiles.Caster.Skyfire;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Skyfire;

/// <summary>天火：蓄满松开发射流星；二技能把落点改为鼠标处的天坠之火。</summary>
public sealed class SkyfireStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [194, 222, 255];
    private static readonly float[] AttackBonus = [.30f, .35f, .40f, .45f, .50f, .55f, .60f, .75f, .90f, 1f];

    // 武器本体暂用原版流星法杖；灰度自绘资源只用于陨石弹幕。
    public override string Texture => "Terraria/Images/Item_" + ItemID.MeteorStaff;
    public override void SetStaticDefaults() => Item.staff[Type] = true;

    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 44;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 20;
        Item.channel = true;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.knockBack = 8f;
        Item.shoot = ModContent.ProjectileType<SkyfireFocus>();
        Item.shootSpeed = 18f;
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(gold: 8);
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<SkyfireStaffPlayer>().TryActivate();
            return false;
        }
        return player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.Skill != 0 || skills.CurrentSkill?.Key.Item != nameof(SkyfireStaff))
            return;
        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        damage *= 1f + AttackBonus[rank];
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<LeonhardtStaff>()
        .AddIngredient(ItemID.Ectoplasm, 16)
        .AddIngredient(ItemID.ChlorophyteBar, 15)
        .AddIngredient(ItemID.SoulofFright, 12)
        .AddTile(TileID.MythrilAnvil)
        .Register();
}
