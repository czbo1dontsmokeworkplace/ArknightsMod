using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Caster.Lava;
using ArknightsMod.Content.Projectiles.Caster.Gitano;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Gitano;

/// <summary>远山的水晶球：蓄力松手施放水晶群攻术式。</summary>
public sealed class GitanoOrb : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [80, 91, 105];
    private static readonly float[] FateBonus = [.3f, .35f, .4f, .45f, .5f, .55f, .7f, .8f, .9f, 1f];
    public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalBall;

    public override void SetDefaults()
    {
        Item.width = Item.height = 32;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 13;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 20;
        Item.channel = true;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.knockBack = 4f;
        Item.shoot = ModContent.ProjectileType<GitanoFocus>();
        Item.shootSpeed = 15f;
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: 2);
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<GitanoOrbPlayer>().TryActivate();
            return false;
        }
        return !player.GetModPlayer<GitanoOrbPlayer>().Exhausted
            && player.ownedProjectileCounts[Item.shoot] == 0 && base.CanUseItem(player);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.Skill != 1
            || skills.CurrentSkill?.Key.Item != nameof(GitanoOrb)) return;
        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        damage *= 1f + FateBonus[rank];
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        Projectile.NewProjectile(source, player.MountedCenter, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<Lava_Dagger>()
        .AddIngredient(ItemID.CrystalShard, 16)
        .AddIngredient(ItemID.SoulofLight, 6)
        .AddTile(TileID.MythrilAnvil)
        .Register();
}
