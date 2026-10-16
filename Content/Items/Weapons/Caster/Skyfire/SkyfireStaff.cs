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

/// <summary>天火：直接发射流星；二技能把落点改为鼠标处的天坠之火。</summary>
public sealed class SkyfireStaff : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [237, 271, 311];
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
        Item.useTime = Item.useAnimation = 65;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.knockBack = 8f;
        Item.shoot = ModContent.ProjectileType<SkyfireMeteor>();
        Item.shootSpeed = 18f;
        Item.UseSound = SoundID.Item73 with { Volume = .58f };
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
        return base.CanUseItem(player);
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
        {
            WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
            bool skyfall = skills.SkillActive && skills.Skill == 1
                && skills.CurrentSkill?.Key.Item == nameof(SkyfireStaff);
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzle = player.MountedCenter + direction * 38f;
            Vector2 start = muzzle;
            Vector2 shotVelocity = direction * (Item.shootSpeed * 1.2f);
            float multiplier = 1f;
            if (skyfall)
            {
                Vector2 target = Main.MouseWorld;
                Vector2 offset = target - player.MountedCenter;
                if (offset.LengthSquared() > 850f * 850f)
                    target = player.MountedCenter + offset.SafeNormalize(Vector2.UnitX) * 850f;
                start = target + new Vector2(-230f, -520f);
                shotVelocity = (target - start).SafeNormalize(Vector2.UnitY) * (22f * 1.2f);
                int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
                multiplier = MathHelper.Lerp(1.5f, 2.35f, rank / 9f);
            }
            int index = Projectile.NewProjectile(source, start, shotVelocity, type,
                Math.Max(1, (int)(damage * multiplier)), knockback, player.whoAmI, skyfall ? 1f : 0f);
            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
            if (!Main.dedServ) SkyfireVisuals.FireParticles(muzzle, direction);
        }
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
