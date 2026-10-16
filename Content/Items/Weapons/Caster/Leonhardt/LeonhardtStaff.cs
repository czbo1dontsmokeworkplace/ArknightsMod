using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Caster.Gitano;
using ArknightsMod.Content.Projectiles.Caster.Leonhardt;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Leonhardt;

/// <summary>莱恩哈特的法杖：发射一枚可引发范围爆破的源石核心。</summary>
public sealed class LeonhardtStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [158, 182, 209];
    private static readonly float[] AttackBonus = [.30f, .35f, .40f, .45f, .50f, .55f, .60f, .75f, .90f, 1f];

    public override void SetStaticDefaults() => Item.staff[Type] = true;

    public override void SetDefaults()
    {
        Item.width = 62;
        Item.height = 66;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 18;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = 65;
        Item.useAnimation = 65;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.knockBack = 6f;
        Item.shoot = ModContent.ProjectileType<LeonhardtCore>();
        Item.shootSpeed = 16f;
        Item.UseSound = SoundID.Item73 with { Volume = .58f };
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(gold: 3);
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<LeonhardtStaffPlayer>().TryActivate();
            return false;
        }
        return base.CanUseItem(player);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.SkillActive && skills.Skill == 0
            && skills.CurrentSkill?.Key.Item == nameof(LeonhardtStaff))
        {
            int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
            damage *= 1f + AttackBonus[rank];
        }
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Vector2 velocity,
        int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzle = player.MountedCenter + direction * 56f;
            int index = Projectile.NewProjectile(source, muzzle, direction * (17f * 1.2f),
                type, damage, knockback, player.whoAmI, 1f);
            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            if (!Main.dedServ) LeonhardtVisuals.FireParticles(muzzle, direction);
        }
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<GitanoOrb>()
        .AddIngredient(ItemID.HallowedBar, 12)
        .AddIngredient(ItemID.CrystalShard, 20)
        .AddIngredient(ItemID.SoulofLight, 8)
        .AddIngredient<global::ArknightsMod.Content.Items.Material.OrironCluster>()
        .AddTile(ModContent.TileType<global::ArknightsMod.Content.Tiles.Infrastructure.FactoryTile>())
        .Register();
}
