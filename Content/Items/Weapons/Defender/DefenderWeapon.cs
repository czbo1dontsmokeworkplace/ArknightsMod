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
        Item.DamageType = DefenderRules.IsFortress(Item) ? DamageClass.Ranged : DamageClass.Melee;
        Item.width = Item.height = 48;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Interval;
        Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.knockBack = 7f;
        Item.crit = Name == "NianWeapon" ? 21 : 2;
        if (Name == "NoirShield") Item.knockBack = 12f;
        Item.shoot = ModContent.ProjectileType<DefenderHoldout>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: 2);
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            player.GetModPlayer<DefenderCombatPlayer>().TryActivate();
            return false;
        }
        return !ShieldGuardRules.IsRaised(player) && !DefenderHoldout.IsBusy(player)
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
        if (!DefenderRules.SkillActive(player)) return;
        int skill = player.GetModPlayer<WeaponPlayer>().Skill;
        damage *= Name switch
        {
            "DN_Weapon" => 1.8f,
            "NianWeapon" => skill == 0 ? 1.45f : skill == 2 ? 2.2f : 1f,
            "Vulcan_Weapon" => skill == 1 ? 1.9f : 1f,
            "LiskarmPistol" => skill == 1 ? 1.8f : 1f,
            "CroissantHammer" => skill == 1 ? 2.2f : 1f,
            "MudrockHammer" => skill == 2 ? 2.4f : skill == 1 ? 1.6f : 1f,
            "EunectesAxe" => skill == 2 ? 2.8f : skill == 1 ? 2f : 1.3f,
            _ => 1f
        };
    }
    public override void AddRecipes() => DefenderRecipes.Register(this);
}
