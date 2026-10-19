using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Vanguard.Spears;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard;

public abstract class VanguardSpearWeapon : ExpansionWeaponBase
{
    public abstract VanguardSpearKind Kind { get; }
    protected abstract int Rarity { get; }
    public override bool MeleePrefix() => true;
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.Spears[Type] = true;
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 54; Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Melee; Item.knockBack = 4 + (int)Kind;
        Item.useTime = Item.useAnimation = 28; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.channel = false; Item.useTurn = false; Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<VanguardSpearHoldout>();
        Item.rare = Rarity; Item.value = Item.sellPrice(gold: 1 + (int)Kind * 3);
    }
    public override bool AltFunctionUse(Player player) => true;
    // Keep the native item timers consistent with the separately simulated thrust duration.
    public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? 1f : 2f;
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player) || player.dead || player.CCed || player.noItems) return false;
        if (player.ownedProjectileCounts[ModContent.ProjectileType<VanguardSpearHoldout>()] != 0
            || player.ownedProjectileCounts[ModContent.ProjectileType<VanguardLanceCharge>()] != 0) return false;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noUseGraphic = true;
        return player.altFunctionUse != 2 || (!player.mount.Active && player.grapCount == 0
            && player.GetModPlayer<VanguardSpearPlayer>().Cooldown.Ready);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        VanguardSpearPlayer state = player.GetModPlayer<VanguardSpearPlayer>();
        bool charge = player.altFunctionUse == 2;
        if (charge && !state.Cooldown.TryStart()) return false;
        int skill = state.AttackSkill(Kind, charge);
        float power = VanguardSpearRules.AttackMultiplier(Kind, skill);
        // One talent roll per action, rather than a different strength on every target.
        bool accented = Kind == VanguardSpearKind.Vigna && Main.rand.NextFloat() < (skill > 0 ? .3f : .1f);
        if (accented) power *= 2;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        int projectileType = charge ? ModContent.ProjectileType<VanguardLanceCharge>()
            : ModContent.ProjectileType<VanguardSpearHoldout>();
        Projectile p = Projectile.NewProjectileDirect(source, player.MountedCenter, aim, projectileType,
            Math.Max(1, (int)(damage * power * (charge ? VanguardSpearRules.ChargeDamage(Kind) : 1f))),
            knockback * (Kind == VanguardSpearKind.WildMane && skill == 2 ? 2 : 1), player.whoAmI, (int)Kind);
        if (p.ModProjectile is VanguardSpearHoldout holdout) holdout.Configure(player, Item, skill, accented);
        if (p.ModProjectile is VanguardLanceCharge lance) lance.Configure(player, Item, accented);
        p.CritChance = player.GetWeaponCrit(Item); p.netUpdate = true;
        return false;
    }
    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.VanguardSpears.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.VanguardSpears.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "VanguardControls", Language.GetTextValue("Mods.ArknightsMod.VanguardSpears.Controls",
            VanguardSpearRules.ChargeDamage(Kind), VanguardChargeCooldown.Duration / 60,
            (int)VanguardSpearRules.ChargeRange(Kind))));
        tooltips.Add(new TooltipLine(Mod, "VanguardSkill", GetSkillActivateKeyHint()));
        if (Kind is VanguardSpearKind.Grani or VanguardSpearKind.WildMane)
            tooltips.Add(new TooltipLine(Mod, "VanguardPlaceholder", Language.GetTextValue("Mods.ArknightsMod.VanguardSpears.Placeholder")));
    }
}
