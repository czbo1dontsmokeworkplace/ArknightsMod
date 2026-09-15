using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Projectiles.Guard.Gavial;
using ArknightsMod.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Weapons.Guard.Gavial;

public sealed class GavialChainsawPlayer : ModPlayer
{
    private readonly GavialDamageDebt debt = new();
    private int mode, remaining, duration, slot, healCooldown, pendingDebt;
    private bool keyWasDown;
    private Item activeItem;
    private bool Holding => Player.HeldItem.ModItem is GavialChainsaw;
    // 切物品的同一帧立即失去强化；不能把加成带给别的武器。
    public int ActiveMode => Holding && Player.HeldItem == activeItem && Player.selectedItem == slot
        && Player.GetModPlayer<WeaponPlayer>().Skill == mode - 1 && remaining > 0 ? mode : 0;

    internal bool TryActivate()
    {
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (Player.whoAmI != Main.myPlayer || !Holding || Player.dead || Player.noItems || Player.CCed
            || mode != 0 || shared.SkillActive || shared.StockCount <= 0 || shared.CurrentSkill == null)
            return false;

        mode = shared.Skill + 1;
        slot = Player.selectedItem;
        activeItem = Player.HeldItem;
        duration = remaining = Math.Max(1, (int)MathF.Round(
            shared.CurrentSkill.CurrentLevelData.ActiveTime * 60 * WeaponPlayer.ActiveDurationMultiplier));
        shared.DelStockCount();
        shared.SkillActive = true;
        shared.SkillTimer = 0;
        GavialVisuals.Activate(Player, mode);
        return true;
    }

    public override void PostUpdateEquips()
    {
        if (ActiveMode == 2)
            Player.statDefense += Math.Max(1, (int)MathF.Round(Player.statDefense * 0.5f));
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return;
        if (healCooldown > 0) healCooldown--;

        if (mode != 0)
        {
            if (ActiveMode == 0 || --remaining <= 0) EndSkill();
            else
            {
                var shared = Player.GetModPlayer<WeaponPlayer>();
                shared.SkillActive = true;
                shared.SkillTimer = duration - remaining;
            }
        }

        bool keyDown = ArknightsKeybinds.SkillActivatePressed(Player);
        if (keyDown && !keyWasDown) TryActivate();
        keyWasDown = keyDown;

        // 流失不走 Hurt：不被防御/无敌帧抵消，也不会被三技能再次延期。
        int loss = debt.Tick();
        if (loss > 0)
        {
            Player.statLife -= loss;
            if (Player.statLife <= 0)
                Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.ArknightsMod.GavialStrainDeath", Player.name)), loss, 0);
        }
        if (debt.Remaining > 0)
            Player.AddBuff(ModContent.BuffType<GavialStrain>(), 2);
    }

    private void EndSkill()
    {
        // WeaponPlayer 可能已经初始化了新武器/新技能；只清理由本技能占用的旧条。
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (Holding && shared.Skill == mode - 1)
        {
            shared.SkillActive = false;
            shared.SkillTimer = 0;
        }
        if (mode == 3) debt.BeginRepayment();
        mode = remaining = duration = pendingDebt = 0;
        activeItem = null;
    }

    internal void HealFromSaw(NPC target, int damageDone)
    {
        if (Player.whoAmI != Main.myPlayer || ActiveMode != 1 || healCooldown > 0
            || damageDone <= 0 || target.friendly || target.lifeMax <= 5 || target.type == Terraria.ID.NPCID.TargetDummy
            || Player.statLife >= Player.statLifeMax2) return;
        int amount = Math.Min(Player.statLifeMax2 - Player.statLife,
            Math.Min(GavialBalance.HealCap, Math.Max(1, (int)(damageDone * 0.04f))));
        Player.Heal(amount);
        healCooldown = GavialBalance.HealCooldownTicks;
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        pendingDebt = 0;
        if (Player.whoAmI != Main.myPlayer || ActiveMode != 3) return;
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            pendingDebt = Math.Max(0, info.Damage / 2);
            info.Damage -= pendingDebt;
        };
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        // 仅在真正受伤后记账，闪避掉的攻击不会凭空留下欠血。
        if (Player.whoAmI == Main.myPlayer && ActiveMode == 3)
            debt.Bank(Math.Min(pendingDebt, Math.Max(0, info.Damage)));
        pendingDebt = 0;
    }

    public override void SaveData(TagCompound tag)
    {
        if (debt.Banked + debt.Remaining > 0)
            tag["GavialDamageDebt"] = debt.Banked + debt.Remaining;
    }

    public override void LoadData(TagCompound tag)
    {
        debt.Reset();
        debt.Bank(tag.GetInt("GavialDamageDebt"));
        debt.BeginRepayment();
    }

    public override void UpdateDead()
    {
        if (mode != 0) EndSkill();
        debt.Reset();
        healCooldown = pendingDebt = 0;
        keyWasDown = false;
    }
}
