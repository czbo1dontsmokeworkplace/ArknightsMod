using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Guard.Reaper;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class ReaperPlayer : ModPlayer
{
    private int mode, remaining, duration, slot, level = 1, healCooldown;
    private Item activeItem;
    private bool keyWasDown, attackCharged, previousRight;
    public bool RightHeld { get; private set; }
    public float AttackPower { get; private set; } = 1;
    private float attackHeal = 1;
    public bool Holding => !Player.dead && Player.HeldItem.ModItem is ReaperWeapon;
    public ReaperKind Kind => Player.HeldItem.ModItem is ReaperWeapon w ? w.Kind : ReaperKind.Humus;
    public int ActiveMode => Holding && remaining > 0 && activeItem == Player.HeldItem && slot == Player.selectedItem
        && Player.GetModPlayer<WeaponPlayer>().Skill == mode - 1 ? mode : 0;
    public bool SuppressAttacks => Kind == ReaperKind.Entelechia && ActiveMode == 2;
    internal int Level => Math.Clamp(Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.ForceReplaceLevel
        ?? Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.Level ?? 1, 1, 10);
    private float L(float first, float last) => MathHelper.Lerp(first, last, (level - 1) / 9f);
    public float AttackSpeed => ActiveMode switch {
        2 when Kind == ReaperKind.LaPluma => L(1.5f, 2),
        3 when Kind == ReaperKind.Entelechia => 1.4f,
        _ => 1 };
    internal float ReachMultiplier => ActiveMode switch {
        3 when Kind == ReaperKind.Entelechia => 1.3f,
        _ => 1 };
    public float DamageMultiplier => ActiveMode switch {
        2 when Kind == ReaperKind.Humus => Player.statLife > Player.statLifeMax2 * .8f ? 1 + L(.21f, .5f) * 2 : Player.statLife > Player.statLifeMax2 * .5f ? 1 + L(.21f, .5f) : 1,
        2 when Kind == ReaperKind.LaPluma => L(1.1f, 1.5f),
        3 when Kind == ReaperKind.Entelechia => 2.35f,
        _ => 1 };

    public override void SetControls()
    {
        RightHeld = Holding && Player.controlUseTile && !Player.mouseInterface && !Main.blockMouse
            && Content.Items.Weapons.Charged.ChargedOperatorWeapon.WorldInputAllowed(Player);
        if (RightHeld) { Player.controlUseTile = false; Player.controlUseItem = false; Player.channel = false; }
        if (!RightHeld && previousRight) { ReaperSpin.Find(Player)?.Projectile.Kill(); Player.itemTime = Player.itemAnimation = 0; Player.releaseUseItem = true; }
        previousRight = RightHeld;
    }
    internal void BeginAttack()
    {
        level = Level; AttackPower = attackHeal = 1; attackCharged = false;
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (shared.Skill == 0 && shared.StockCount > 0 && ActiveMode == 0) {
            shared.DelStockCount();
            AttackPower = Kind switch { ReaperKind.Humus => L(1.6f, 2.6f), ReaperKind.LaPluma => L(1.3f, 1.65f), _ => 1.75f };
            attackHeal = Kind == ReaperKind.Humus ? 2 : 1;
        }
    }
    internal void ModifyCut(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == ReaperKind.LaPluma && ActiveMode == 2 && target.life < target.lifeMax / 2) modifiers.FinalDamage *= 1.3f;
    }
    internal void OnCut(NPC target, int damage, bool physical)
    {
        if (Player.whoAmI != Main.myPlayer || !Holding || damage <= 0 || target.friendly || target.lifeMax <= 5 || target.type == NPCID.TargetDummy) return;
        // Ranged scythes and blood circles never enter the healing path.
        if (!physical) return;
        CreditAttack();
        if (healCooldown > 0 || Player.statLife >= Player.statLifeMax2 || Player.moonLeech) return;
        int cap = ReaperRules.HealCap(Kind);
        int heal = Math.Min(Player.statLifeMax2 - Player.statLife, (int)(Math.Clamp((int)(damage * .025f), 1, cap) * attackHeal));
        if (heal <= 0) return;
        Player.Heal(heal); healCooldown = 12;
        ReaperVisuals.Drain(target.Center, Player.Center, Kind);
    }
    internal void OnRightHit(NPC target, int damage)
    {
        if (Player.whoAmI != Main.myPlayer || !Holding || damage <= 0 || target.friendly || target.lifeMax <= 5 || target.type == NPCID.TargetDummy) return;
        OnCut(target, damage, false);
        CreditAttack();
    }
    private void CreditAttack()
    {
        if (!attackCharged) {
            attackCharged = true;
            var shared = Player.GetModPlayer<WeaponPlayer>();
            if (shared.CurrentSkill?.ChargeType == Systems.Gameplay.Skill.SkillChargeType.Attack) shared.OffensiveRecovery();
        }
    }
    internal bool TryActivate()
    {
        var shared = Player.GetModPlayer<WeaponPlayer>();
        bool keyDown = ArknightsKeybinds.SkillActivatePressed(Player);
        if (!Holding || Player.whoAmI != Main.myPlayer || Player.noItems || Player.CCed || keyDown && keyWasDown) return false;
        if (ActiveMode > 0) return false;
        if (shared.CurrentSkill == null || shared.StockCount <= 0 || shared.SkillActive || shared.Skill == 0) return false;
        activeItem = Player.HeldItem; slot = Player.selectedItem; level = Level; mode = shared.Skill + 1;
        duration = remaining = Math.Max(1, (int)(shared.CurrentSkill.CurrentLevelData.ActiveTime * 60 * WeaponPlayer.ActiveDurationMultiplier));
        shared.DelStockCount(); shared.SkillActive = true; shared.SkillTimer = 0; keyWasDown = keyDown;

        if (SuppressAttacks) {
            ReaperSwing.Find(Player)?.Projectile.Kill();
            ReaperSpin.Find(Player)?.Projectile.Kill();
            for (int i = 0; i < 2; i++) Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<ReaperBloodOrbit>(), (int)(Player.GetWeaponDamage(Player.HeldItem) * 1.95f), 3, Player.whoAmI, i, slot);
        }
        ReaperVisuals.Activate(Player, Kind); return true;
    }
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (healCooldown > 0) healCooldown--;
        if (mode != 0) {
            if (ActiveMode == 0 || Player.noItems || Player.CCed) EndSkill();
            else if (!Systems.Gameplay.MujicaFeverSystem.PauseSkill(Player) && --remaining <= 0) EndSkill();
            else { var shared = Player.GetModPlayer<WeaponPlayer>(); shared.SkillActive = true; shared.SkillTimer = duration - remaining; }
        }
        bool keyDown = ArknightsKeybinds.SkillActivatePressed(Player);
        if (keyDown && !keyWasDown) TryActivate();
        keyWasDown = keyDown;
    }
    private void EndSkill()
    {
        bool same = Holding && activeItem == Player.HeldItem && Player.GetModPlayer<WeaponPlayer>().Skill == mode - 1;
        if (same) { var shared = Player.GetModPlayer<WeaponPlayer>(); shared.SkillActive = false; shared.SkillTimer = 0; }
        mode = remaining = duration = 0; activeItem = null; AttackPower = attackHeal = 1;
    }
    public override void UpdateDead() { EndSkill(); healCooldown = 0; keyWasDown = previousRight = RightHeld = false; }
}
