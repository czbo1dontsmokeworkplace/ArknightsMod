using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster;

/// <summary>The two earlier mech-accord staves share Goldenglow's raised, channeled control scheme.</summary>
public abstract class MechAccordStaffBase : VerticalStaffBase
{
    internal abstract bool IsRockrock { get; }
    internal abstract float SearchRadius { get; }
    internal abstract float Reach { get; }
    internal abstract int PulseInterval { get; }

    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Magic;
        Item.width = Item.height = 54;
        Item.useTime = Item.useAnimation = 14;
        Item.mana = IsRockrock ? 6 : 4;
        Item.knockBack = IsRockrock ? 3f : 2f;
        Item.crit = 4;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.shoot = ModContent.ProjectileType<MechAccordHeldStaff>();
        Item.shootSpeed = 1f;
        ApplyVerticalStaffPose();
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (ArknightsKeybinds.SkillActivatePressed(player))
            ActivateSkill(player);
    }

    private static void ActivateSkill(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill == null || skills.SkillActive || skills.StockCount <= 0) return;
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        skills.DelStockCount();
        SoundEngine.PlaySound(SoundID.Item122 with { Volume = .38f, MaxInstances = 2 }, player.Center);
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            ActivateSkill(player);
            return false;
        }
        if(!IsRockrock)
            foreach(Projectile p in Main.ActiveProjectiles)
                if(p.owner==player.whoAmI&&p.ModProjectile is ClickDrone drone&&drone.Busy)return false;
        return player.ownedProjectileCounts[ModContent.ProjectileType<MechAccordHeldStaff>()] == 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage,
                knockback, player.whoAmI, 0f, player.selectedItem);
        return false;
    }

    internal static int SkillRank(WeaponPlayer skills) => Math.Clamp(
        (skills.CurrentSkill?.ForceReplaceLevel ?? skills.CurrentSkill?.Level ?? 1) - 1, 0, 9);

    internal virtual float AttackSpeedBonus(WeaponPlayer skills) => 1f;
    internal virtual float DroneCap(WeaponPlayer skills) => 1.1f;
    internal virtual float DroneDamageBonus(WeaponPlayer skills) => 1f;

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.SkillActive && skills.Skill == 0 && !IsRockrock)
            damage *= 1.2f + .025f * SkillRank(skills);
        else if (skills.SkillActive && skills.Skill == 1 && !IsRockrock)
            damage *= 1.35f + .035f * SkillRank(skills);
    }
}
