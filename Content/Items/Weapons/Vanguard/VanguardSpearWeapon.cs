using System;
using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Vanguard.Spears;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Audio;
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
        if (player.ownedProjectileCounts[ModContent.ProjectileType<VanguardSpearHoldout>()] != 0) return false;
        Item.useStyle = player.altFunctionUse == 2 ? ItemUseStyleID.Swing : ItemUseStyleID.Shoot;
        Item.noUseGraphic = true;
        return player.altFunctionUse != 2 || player.GetModPlayer<VanguardSpearPlayer>().Reserve.Ready;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        VanguardSpearPlayer state = player.GetModPlayer<VanguardSpearPlayer>();
        bool volley = player.altFunctionUse == 2;
        if (volley && !state.Reserve.TrySpend()) return false;
        int skill = state.AttackSkill(Kind, volley);
        float power = VanguardSpearRules.AttackMultiplier(Kind, skill);
        // One talent roll per action, rather than a different strength on every target.
        bool accented = Kind == VanguardSpearKind.Vigna && Main.rand.NextFloat() < (skill > 0 ? .3f : .1f);
        if (accented) power *= 2;
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 origin = player.MountedCenter;
        if (volley)
        {
            player.ChangeDir(aim.X < 0 ? -1 : 1);
            // Native Swing animation owns the throwing arm. No held projectile is created.
            Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full,
                aim.ToRotation() - MathHelper.PiOver2);
            Vector2 offset = hand - origin + aim * 10;
            origin += Collision.TileCollision(origin - new Vector2(3), offset, 6, 6);
            type = ModContent.ProjectileType<VanguardThrownSpear>();
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { Volume = .85f, Pitch = -.3f }, origin);
        }
        Projectile p = Projectile.NewProjectileDirect(source, origin,
            volley ? aim * VanguardSpearRules.FlightSpeed(Kind) / VanguardSpearRules.FlightUpdates : aim, type,
            Math.Max(1, (int)(damage * power * (volley ? VanguardSpearRules.ThrowDamage(Kind) : 1f))),
            knockback * (Kind == VanguardSpearKind.WildMane && skill == 2 ? 2 : 1), player.whoAmI, (int)Kind, volley ? 1 : 0);
        if (p.ModProjectile is VanguardSpearHoldout holdout) holdout.Configure(player, Item, skill, accented);
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
            VanguardSpearRules.ThrowDamage(Kind), VanguardReserve.Capacity / 60)));
        tooltips.Add(new TooltipLine(Mod, "VanguardSkill", GetSkillActivateKeyHint()));
        if (Kind is VanguardSpearKind.Grani or VanguardSpearKind.WildMane)
            tooltips.Add(new TooltipLine(Mod, "VanguardPlaceholder", Language.GetTextValue("Mods.ArknightsMod.VanguardSpears.Placeholder")));
    }
}
