using System;
using System.Collections.Generic;
using ArknightsMod.Content;
using ArknightsMod.Content.Buffs.Supporter.Magallan;
using ArknightsMod.Content.Projectiles.Supporter.Magallan;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Magallan;

public sealed class MagallanController : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [72, 80, 90];

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 54;
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10;
        Item.knockBack = 3f;
        Item.useTime = Item.useAnimation = 25;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.noMelee = true;
        Item.sentry = false;
        Item.shoot = ModContent.ProjectileType<MagallanDrone>();
        Item.shootSpeed = 0f;
        Item.buffType = ModContent.BuffType<MagallanDroneBuff>();
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(gold: 8);
    }

    public override bool AltFunctionUse(Player player) => true;
    public override bool? CanAutoReuseItem(Player player) => false;

    public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
    {
        if (player.altFunctionUse == 2) mult = 0f;
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
            return false;
        return player.altFunctionUse == 2 || player.maxMinions >= 1f;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var control = player.GetModPlayer<MagallanPlayer>();
        if (player.altFunctionUse == 2)
        {
            control.RecallDrones();
            return false;
        }

        control.UpdateCommand();
        // Air placement: no ground search, gravity or artificial altitude limit.
        position = Main.MouseWorld;
        position.X = MathHelper.Clamp(position.X, 32f, Main.maxTilesX * 16f - 32f);
        position.Y = MathHelper.Clamp(position.Y, 32f, Main.maxTilesY * 16f - 32f);
        int id = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback,
            player.whoAmI, control.Module, 1f);
        if (id < Main.maxProjectiles)
        {
            Main.projectile[id].originalDamage = Item.damage + EliteDamage[Math.Clamp(EliteStage, 0, 2)] - EliteDamage[0];
            player.AddBuff(Item.buffType, 2);
        }
        SoundEngine.PlaySound(SoundID.Item44 with { Volume = 0.65f }, position);
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        int mode = Main.LocalPlayer.GetModPlayer<MagallanPlayer>().Module;
        tooltips.Add(new TooltipLine(Mod, "MagallanModule", Language.GetTextValue(
            "Mods.ArknightsMod.Items.MagallanController.CurrentModule",
            Language.GetTextValue($"Mods.ArknightsMod.Skills.MagallanController.{mode + 1}.Label")))
        { OverrideColor = MagallanVisuals.ModuleColor(mode) });
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.JimsDrone)
        .AddIngredient(ItemID.BeetleHusk, 6)
        .AddIngredient(ItemID.ChlorophyteBar, 12)
        .AddIngredient(ItemID.Wire, 30)
        .AddTile(TileID.MythrilAnvil)
        .AddCondition(Condition.DownedGolem)
        .Register();
}

// Commands require the controller; deployed sentries retain the last command when it is put away.
public sealed class MagallanPlayer : ModPlayer
{
    private bool holdingController;
    public int Module { get; private set; }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (Player.dead || Player.HeldItem.ModItem is not MagallanController controller)
        {
            holdingController = false;
            return;
        }
        var skills = Player.GetModPlayer<WeaponPlayer>();
        if (!holdingController)
        {
            if (!skills.TrySelectSkill(controller, 0, force: true)) return;
            holdingController = true;
        }
        UpdateCommand();
    }

    public void UpdateCommand()
    {
        if (Player.whoAmI != Main.myPlayer || Player.HeldItem.ModItem is not MagallanController) return;
        var skills = Player.GetModPlayer<WeaponPlayer>();
        int next = Math.Clamp(skills.Skill, 0, 2);
        bool changed = next != Module;
        Module = next;
        if (skills.CurrentSkill != null)
        {
            // Every selected module starts active; the skill bar has no manual activation state.
            skills.SkillActive = true;
            skills.SkillTimer = 0;
            skills.StockCount = 0;
            skills.SP = 0;
        }
        if (changed) Broadcast();
    }

    public void RecallDrones()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return;
        bool recalled = false;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Player.whoAmI || p.ModProjectile is not MagallanDrone drone || drone.Recalling) continue;
            drone.BeginRecall();
            recalled = true;
        }
        if (recalled) SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.65f, Pitch = 0.15f }, Player.Center);
    }

    private void Broadcast()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Player.whoAmI || p.ModProjectile is not MagallanDrone drone) continue;
            if (!drone.Recalling) drone.ApplyCommand(Module);
        }
    }
}
