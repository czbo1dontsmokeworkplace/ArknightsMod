using System;
using ArknightsMod.Content.Projectiles.Defender;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Defender;

// All defenders use one held template; item names and inventory textures stay stable.
public abstract class DefenderWeapon : ExpansionWeaponBase
{
    protected virtual int Interval => 30;
    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DefenderRules.IsFortress(Item) ? DamageClass.Ranged
            : DefenderRules.IsSentinel(Item) ? DamageClass.Magic : DamageClass.Melee;
        Item.width = Item.height = 48;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Interval;
        Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.knockBack = 7f;
        Item.crit = Name == "NianWeapon" ? 21 : 2;
        if (Name == "NoirShield") Item.knockBack = 12f;
        Item.shoot = ModContent.ProjectileType<DefenderHoldout>();
        Item.shootSpeed = 1f;
        int tier = DefenderRouteCatalog.Find(Name)?.Tier ?? 2;
        Item.rare = tier switch { 0 => ItemRarityID.White, 1 => ItemRarityID.Blue, 2 => ItemRarityID.Orange, 3 => ItemRarityID.Pink, _ => ItemRarityID.Lime };
        Item.value = Item.sellPrice(gold: Math.Max(1, tier * 2));
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<DefenderCombatPlayer>().TryActivate();
            return false;
        }
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (DefenderSkillRules.UsesOneAttack(Name, skills.Skill) && skills.CurrentSkill?.Key.Item == Name
            && !skills.SkillActive && skills.StockCount > 0 && !ShieldGuardRules.WantsRaised(player)
            && !DefenderHoldout.IsBusy(player) && !player.GetModPlayer<EunectesDashPlayer>().Dashing)
            player.GetModPlayer<DefenderCombatPlayer>().TryActivate();
        return !ShieldGuardRules.WantsRaised(player) && !DefenderHoldout.IsBusy(player)
            && !player.GetModPlayer<DefenderCombatPlayer>().StopsAttacking
            && !player.GetModPlayer<EunectesDashPlayer>().Dashing;
    }
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        Item.DamageType = DefenderRules.AttackClass(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            DefenderHoldout.Ensure(player)?.BeginAttack(damage, knockback);
        return false;
    }
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        damage *= DefenderSkillRules.Stats(player).Attack;
    }
    public override float UseSpeedMultiplier(Player player) => DefenderSkillRules.Stats(player).Speed;
    public override void AddRecipes() => DefenderRecipes.Register(this);
}
