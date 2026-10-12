using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Caster.Leizi;
using ArknightsMod.Content.Projectiles.Caster.Pudding;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Pudding;

/// <summary>骷髅王后的布丁电磁法杖；二连发链电，每发击中实体后最多再弹跳三次。</summary>
public sealed class PuddingStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [68, 74, 80];
    private static readonly float[] ChantSpeed = [.15f, .20f, .25f, .30f, .35f, .40f, .45f, .55f, .65f, .75f];
    private static readonly float[] DiffusionDamage = [.30f, .35f, .45f, .50f, .55f, .60f, .80f, .90f, 1f, 1.10f];
    public override void SetStaticDefaults() => Item.staff[Type] = true;

    public override void SetDefaults()
    {
        Item.width = Item.height = 64;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.mana = 7;
        Item.useTime = 8;
        Item.useAnimation = 32;
        Item.useLimitPerAnimation = 2;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.autoReuse = true;
        Item.knockBack = 2f;
        Item.crit = 4;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(silver: 75);
        Item.shoot = ModContent.ProjectileType<PuddingFlyingDischarge>();
        Item.shootSpeed = LeiziFlyingDischarge.SpeedPerUpdate;
        Item.UseSound = SoundID.Item93 with { Volume = .27f, Pitch = .42f };
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<PuddingStaffPlayer>().TryActivate();
            return false;
        }
        return base.CanUseItem(player);
    }

    public override float UseSpeedMultiplier(Player player)
    {
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.Skill != 0
            || skills.CurrentSkill?.Key.Item != nameof(PuddingStaff)) return 1f;
        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        return 1f + ChantSpeed[rank];
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (!skills.SkillActive || skills.Skill != 1
            || skills.CurrentSkill?.Key.Item != nameof(PuddingStaff)) return;
        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        damage *= 1f + DiffusionDamage[rank];
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        int mode = skills.SkillActive && skills.CurrentSkill?.Key.Item == nameof(PuddingStaff)
            && skills.Skill is 0 or 1 ? skills.Skill + 1 : 0;
        Vector2 direction = (Main.MouseWorld - position).SafeNormalize(new Vector2(player.direction, 0f));
        int index = Projectile.NewProjectile(source, position,
            direction * LeiziFlyingDischarge.SpeedPerUpdate, type, damage, knockback, player.whoAmI, mode);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.MagicMissile)
        .AddIngredient(ItemID.Bone, 25)
        .AddIngredient(ItemID.Wire, 20)
        .AddIngredient(ItemID.MeteoriteBar, 12)
        .AddIngredient(ItemID.Sapphire, 8)
        .AddTile(TileID.Anvils)
        .Register();
}
