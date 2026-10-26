using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using ArknightsMod.Players;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

public sealed class StandardBearerPlayer : ModPlayer
{
    internal StandardBearerSkill Active;
    private bool keyDown;
    private int previousType = -1, previousSkill = -1;
    private readonly Dictionary<(int, int), (int charge, int stock, int sp)> reserves = new();
    public static bool IsPhased(Player player) => player.active && !player.dead
        && player.GetModPlayer<StandardBearerPlayer>().Active is { } active && active.Valid(player);
    internal void EnforceFlag() => Active?.Enforce(Player);
    public override void PreUpdate() => EnforceFlag();
    public override void SetControls() => EnforceFlag();
    public override bool PreItemCheck() { EnforceFlag(); return true; }
    public override bool ShiftClickSlot(Item[] inventory, int context, int slot) => IsPhased(Player)
        && ReferenceEquals(inventory, Player.inventory) && slot == Active.BoundSlot;
    public static bool Allied(Player source, Player target) => target.active && !target.dead
        && (source.whoAmI == target.whoAmI || (!source.hostile && !target.hostile)
            || (source.team != 0 && source.team == target.team));
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return;
        EnforceFlag();
        bool key = ArknightsKeybinds.SkillActivatePressed(Player);
        if (Player.HeldItem.ModItem is not StandardBearerWeapon weapon)
        { keyDown = key; previousType = previousSkill = -1; return; }
        WeaponPlayer mp = Player.GetModPlayer<WeaponPlayer>();
        if (mp.CurrentSkill?.Key.Item != weapon.Name) return;
        // Public weapons normally regrant InitSP on every switch. For emergency flags,
        // retain each skill's resource state so slot/item switching cannot mint invulnerability.
        if (previousType != Player.HeldItem.type || previousSkill != mp.Skill)
        {
            if (reserves.TryGetValue((Player.HeldItem.type, mp.Skill), out var old))
            { mp.SkillCharge = old.charge; mp.StockCount = old.stock; mp.SP = old.sp; }
            previousType = Player.HeldItem.type; previousSkill = mp.Skill;
        }
        if (key && !keyDown && !Player.CCed && !Player.noItems && !mp.SkillActive && mp.StockCount > 0)
        {
            mp.DelStockCount(); mp.SkillActive = true; mp.SkillTimer = 0;
            int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<StandardBearerSkill>(), 0, 0, Player.whoAmI, (int)weapon.Kind, mp.Skill);
            if (index < Main.maxProjectiles && Main.projectile[index].ModProjectile is StandardBearerSkill skill)
                skill.Configure(Player, (int)(mp.CurrentSkill.CurrentLevelData.ActiveTime * 60));
            else mp.SkillActive = false;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .5f }, Player.Center);
        }
        keyDown = key;
        reserves[(Player.HeldItem.type, mp.Skill)] = (mp.SkillCharge, mp.StockCount, mp.SP);
    }
    public override void UpdateDead()
    { Active = null; reserves.Clear(); previousType = previousSkill = -1; keyDown = false; }
    public override void PostUpdateEquips()
    {
        if (Player.dead) return;
        int tier = -1; bool healingStandard = false;
        foreach (Player source in Main.ActivePlayers)
        {
            if (source.CCed || source.noItems || !Allied(source, Player)
                || source.HeldItem.ModItem is not StandardBearerWeapon weapon
                || source.Distance(Player.Center) > StandardBearerRules.AuraRange) continue;
            tier = Math.Max(tier, (int)weapon.Kind);
            if (IsPhased(source) && source.GetModPlayer<StandardBearerPlayer>().Active.Skill == 1
                && weapon.Kind is StandardBearerKind.Jacint or StandardBearerKind.Saileach) healingStandard = true;
        }
        if (tier >= 0)
        {
            Player.moveSpeed += .05f + tier * .02f;
            Player.statDefense += 2 + tier * 2;
            Player.lifeRegen += 2;
            Player.GetDamage(DamageClass.Summon) += .03f + tier * .02f;
            if (healingStandard) Player.endurance += .06f;
        }
        if (IsPhased(Player)) { Player.thorns = 0; Player.starCloakItem = null; }
    }
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => IsPhased(Player);
    public override bool FreeDodge(Player.HurtInfo info) => IsPhased(Player);
    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !IsPhased(Player);
    public override bool CanBeHitByProjectile(Projectile proj) => !IsPhased(Player);
    public override void UpdateLifeRegen()
    { if (IsPhased(Player) && Player.lifeRegen < 0) { Player.lifeRegen = 0; Player.lifeRegenCount = 0; } }
    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        if (!IsPhased(Player)) return true;
        Player.statLife = Math.Max(1, Player.statLife); return false;
    }
    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        if (Player.HeldItem.ModItem is not StandardBearerWeapon weapon) return;
        bool phased = IsPhased(Player);
        if (phased) { a *= .38f; r *= .8f; b = Math.Min(1, b * 1.2f); }
        float seconds = Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.CurrentLevelData.ActiveTime ?? 30;
        int interval = phased ? ManagedSkillVisualCatalog.Interval(seconds) : 12;
        if (drawInfo.shadow != 0 || Main.gamePaused || !Main.rand.NextBool(interval)) return;
        // Same body-rectangle emission approach as Calamity Buffs/Shadowflame.DrawEffects;
        // use vanilla dust, our own colors/motion, and no dependency on Calamity assets.
        Color tint = StandardBearerRules.Tint(weapon.Kind);
        Dust dust = Dust.NewDustDirect(drawInfo.Position, Player.width, Player.height, DustID.TintableDustLighted,
            Player.velocity.X * .2f, -.8f, 110, tint, phased ? 1.1f : .6f);
        dust.noGravity = true; dust.velocity *= .65f;
        dust.velocity.Y -= phased ? 1.2f : .4f;
    }
}

public sealed class StandardBearerDamageGate : GlobalProjectile
{
    public override bool? CanDamage(Projectile projectile) => (uint)projectile.owner < Main.maxPlayers
        && StandardBearerPlayer.IsPhased(Main.player[projectile.owner]) ? false : null;
    public override bool CanHitPlayer(Projectile projectile, Player target) => (uint)projectile.owner >= Main.maxPlayers
        || !StandardBearerPlayer.IsPhased(Main.player[projectile.owner]);
}

public sealed class StandardBearerItemDamageGate : GlobalItem
{
    public override bool? CanHitNPC(Item item, Player player, NPC target) => StandardBearerPlayer.IsPhased(player) ? false : null;
}
