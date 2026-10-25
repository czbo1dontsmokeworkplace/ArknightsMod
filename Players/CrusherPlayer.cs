using System;
using System.IO;
using ArknightsMod.Content;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Guard.Crusher;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class CrusherPlayer : ModPlayer
{
    internal const float TraitMultiplier = 1.3f;
    private int regenerationRemainder;
    private int mode, remaining, duration, slot, level = 1, skillItemType, drainTicks;
    private bool keyWasDown, hasReturn;
    private Item activeItem;
    private Vector2 returnPosition;
    internal int Activation { get; private set; }
    public bool Holding => Player.active && !Player.dead && Player.HeldItem.ModItem is CrusherWeapon;
    public CrusherKind Kind => Player.HeldItem.ModItem is CrusherWeapon w ? w.Kind : CrusherKind.Quartz;
    public int ActiveMode => Holding && remaining > 0 && Player.selectedItem == slot && Player.HeldItem.type == skillItemType
        && (Player.whoAmI != Main.myPlayer || (Player.HeldItem == activeItem && Player.GetModPlayer<WeaponPlayer>().Skill == mode - 1)) ? mode : 0;
    public bool Charging => ActiveMode == 2 && Kind == CrusherKind.WindChimes;
    internal float Charge => Charging ? Math.Clamp(1 - remaining / (float)Math.Max(1, duration), 0, 1) : 0;
    private float L(float a, float b) => CrusherRules.Level(level, a, b);
    public float SwingSpeed => ActiveMode switch {
        2 when Kind == CrusherKind.Quartz => L(1.35f, 1.8f),
        2 when Kind == CrusherKind.Hoederer => 1 / 1.2f, _ => 1 };
    internal float ReachMultiplier => Kind == CrusherKind.Hoederer && ActiveMode == 3 ? 1.2f : 1;
    internal float DamageMultiplier => (Kind == CrusherKind.Hoederer && Player.GetModPlayer<WeaponPlayer>().Skill == 1 ? L(1.04f, 1.4f) : 1)
        * (ActiveMode switch {
            1 when Kind == CrusherKind.Quartz => L(1.2f, 1.8f),
            2 when Kind == CrusherKind.Quartz => L(.8f, 1.2f),
            1 when Kind == CrusherKind.WindChimes => L(1.3f, 2),
            3 when Kind == CrusherKind.Hoederer => L(1.3f, 2.2f),
            2 when Kind == CrusherKind.Ulpianus => L(2, 2.6f),
            3 when Kind == CrusherKind.Ulpianus => L(2.7f, 3.6f), _ => 1 });

    internal int SkillLevel => Math.Clamp(Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.ForceReplaceLevel
        ?? Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.Level ?? 1, 1, 10);

    public override void PostUpdateEquips()
    {
        if (!Holding) return;
        float hp = ActiveMode switch { 3 when Kind == CrusherKind.Hoederer => L(1.15f, 1.6f),
            2 when Kind == CrusherKind.Ulpianus => L(1.2f, 1.6f), 3 when Kind == CrusherKind.Ulpianus => L(1.2f, 1.8f), _ => 1 };
        Player.statLifeMax2 = Math.Max(1, (int)MathF.Round(Player.statLifeMax2 * TraitMultiplier * hp));
    }
    public override void PostUpdateMiscEffects() { if (Holding) Player.statDefense *= 0; }
    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
    {
        if (Holding && item.healLife > 0 && item.consumable) healValue = (int)MathF.Round(healValue * TraitMultiplier);
    }
    internal int ApplyRegenerationBonus(int regen, int extra)
    {
        if (!Holding) { regenerationRemainder = 0; return regen; }
        // Preserve tenths across ticks: even +1 regeneration receives the full 30% bonus.
        int numerator = (Math.Max(0, regen) + extra) * 3 + regenerationRemainder;
        regenerationRemainder = numerator % 10;
        return regen + numerator / 10;
    }
    public override void NaturalLifeRegen(ref float regen)
    {
        if (!Holding) return;
        // Terraria also scales natural regeneration with maximum life. Cancel the branch's
        // incidental max-life scaling before applying the 30% regeneration bonus below.
        float boosted = Player.statLifeMax2 / 400f * .85f + .15f;
        float normal = Player.statLifeMax2 / TraitMultiplier / 400f * .85f + .15f;
        regen *= normal / boosted;
    }
    public override void UpdateLifeRegen()
    {
        if (Kind == CrusherKind.Ulpianus && ActiveMode == 2) Player.lifeRegen += (int)L(4, 8);
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!Holding) return;
        Player.statDefense *= 0;
        if (Kind == CrusherKind.Quartz && ActiveMode == 2) modifiers.FinalDamage *= 1.25f;
        if (Charging) modifiers.FinalDamage *= 1 - L(.2f, .35f);
    }
    public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
    {
        if (Kind == CrusherKind.Hoederer && ActiveMode == 3) CrusherCombatNet.Contact(Player, npc);
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        bool keyDown = ArknightsKeybinds.SkillActivatePressed(Player);
        if (keyDown && !keyWasDown && Holding) TryActivate();
        keyWasDown = keyDown;
        if (mode != 0) {
            if (ActiveMode == 0 || Player.noItems || Player.CCed) EndSkill(false);
            else {
                var shared = Player.GetModPlayer<WeaponPlayer>();
                shared.SkillActive = true; shared.SkillTimer = duration - remaining;
                if (!shared.CurrentSkill.IsPermanent && !Systems.Gameplay.MujicaFeverSystem.PauseSkill(Player) && --remaining <= 0) EndSkill(true);
                if (ActiveMode == 3 && Kind == CrusherKind.Hoederer && ++drainTicks >= 60) {
                    drainTicks = 0; Player.statLife -= Math.Max(1, Player.statLifeMax2 / 100);
                    if (Player.statLife <= 0) Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.ArknightsMod.CrusherStrainDeath", Player.name)), 1, 0);
                }
            }
        }
        if (Holding && Kind == CrusherKind.Ulpianus && ActiveMode == 0 && Player.GetModPlayer<WeaponPlayer>() is { Skill: 1, StockCount: > 0 }) TryActivate();
        // Max life is a capacity bonus. Equipping/re-equipping never grants current life.
        if (Player.statLife > Player.statLifeMax2) Player.statLife = Player.statLifeMax2;
    }
    internal bool TryActivate()
    {
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (Player.whoAmI != Main.myPlayer || !Holding || Player.noItems || Player.CCed) return false;
        bool keyDown = ArknightsKeybinds.SkillActivatePressed(Player);
        if (keyDown && keyWasDown) return false;
        if (ActiveMode != 0) {
            if (Charging || Kind == CrusherKind.Ulpianus && ActiveMode == 3 || Kind == CrusherKind.Hoederer && ActiveMode == 2) { keyWasDown = keyDown; EndSkill(true); return true; }
            return false;
        }
        if (mode != 0 || shared.StockCount <= 0 || shared.CurrentSkill == null || shared.SkillActive) return false;
        if (Kind == CrusherKind.Hoederer && shared.Skill == 0 || Kind == CrusherKind.Ulpianus && shared.Skill == 0) return false;
        mode = shared.Skill + 1; slot = Player.selectedItem; activeItem = Player.HeldItem; skillItemType = activeItem.type;
        level = SkillLevel; drainTicks = 0; Activation++;
        duration = remaining = Math.Max(1, (int)MathF.Round(shared.CurrentSkill.CurrentLevelData.ActiveTime * 60 * WeaponPlayer.ActiveDurationMultiplier));
        if (shared.CurrentSkill.IsPermanent) duration = remaining = int.MaxValue;
        shared.DelStockCount(); shared.SkillActive = true; shared.SkillTimer = 0;
        if (Charging) CrusherHeldBlade.Find(Player)?.StopForCharge();
        if (Kind == CrusherKind.Ulpianus && mode == 3) {
            returnPosition = Player.position; hasReturn = true;
            Vector2 aim = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(new Vector2(Player.direction, 0));
            Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.MountedCenter, aim * 18,
                ModContent.ProjectileType<CrusherAnchorCast>(), (int)(Player.GetWeaponDamage(Player.HeldItem) * L(1.3f, 1.6f)), 10, Player.whoAmI, 1, slot, level);
        }
        keyWasDown = keyDown; CrusherVisuals.Activate(Player, Kind); SyncAura(); return true;
    }
    internal float PrepareSwing(out int special, bool fullCharge = true)
    {
        special = 0;
        var shared = Player.GetModPlayer<WeaponPlayer>();
        level = SkillLevel;
        if (ActiveMode == 0 && shared.StockCount > 0 && shared.Skill == 0 && (Kind == CrusherKind.Hoederer || Kind == CrusherKind.Ulpianus && fullCharge)) {
            shared.DelStockCount(); special = Kind == CrusherKind.Hoederer ? 1 : 2;
            // Contact That Must Be Made now empowers the on-contact rising anchor.
            // Preparing a swing must never emit a projectile on a miss.
        }
        return DamageMultiplier * (special == 1 ? L(1.6f, 2.6f) : 1);
    }
    internal void Hit(NPC target, bool firstHit, int special, int skillMode)
    {
        if (Player.whoAmI != Main.myPlayer || !firstHit) return;
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (shared.CurrentSkill?.ChargeType == Systems.Gameplay.Skill.SkillChargeType.Attack) shared.OffensiveRecovery();
        if (target.lifeMax <= 5 || target.type == NPCID.TargetDummy) return;
        if (special == 1) HealFraction(L(.1f, .25f));
        if (Kind == CrusherKind.Hoederer && skillMode == 3) HealFraction(.05f);
    }
    internal void ApplyHitEffects(NPC target, int skillMode, int skillLevel)
    {
        if (!target.boss && target.realLife < 0 && target.knockBackResist > 0) {
            if (Kind == CrusherKind.Quartz && skillMode == 2 && Main.rand.NextBool(4)) OperatorStunNPC.TryApply(target, 120);
            if (Kind == CrusherKind.Hoederer && skillMode == 2) OperatorStunNPC.TryApply(target, 60);
            if (Kind == CrusherKind.Hoederer && skillMode == 3 && Main.rand.NextBool(4)) OperatorStunNPC.TryApply(target, (int)CrusherRules.Level(skillLevel, 120, 300));
        }
        if (Kind == CrusherKind.Hoederer && skillMode == 3 && ActiveMode == 3) Mark(target);
    }
    private void Mark(NPC target) => target.GetGlobalNPC<CrusherEmberNPC>().Mark(Player.whoAmI, Activation, level);
    internal void MarkAttacker(NPC target) { if (Kind == CrusherKind.Hoederer && ActiveMode == 3) Mark(target); }
    private void HealFraction(float fraction)
    {
        int heal = Math.Min(Player.statLifeMax2 - Player.statLife, (int)(Player.statLifeMax2 * fraction));
        if (heal > 0) Player.Heal(heal);
    }
    private void EndSkill(bool release)
    {
        bool same = Holding && Player.HeldItem == activeItem && Player.GetModPlayer<WeaponPlayer>().Skill == mode - 1;
        if (release && same && mode == 2 && Kind == CrusherKind.WindChimes) {
            float charge = Math.Clamp(1 - remaining / (float)Math.Max(1, duration), 0, 1);
            var blade = CrusherHeldBlade.Find(Player);
            if (blade == null) {
                int id = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.MountedCenter, Vector2.UnitX,
                    ModContent.ProjectileType<CrusherHeldBlade>(), Player.GetWeaponDamage(Player.HeldItem), 12, Player.whoAmI, (int)Kind, slot);
                if (id >= 0 && id < Main.maxProjectiles) blade = Main.projectile[id].ModProjectile as CrusherHeldBlade;
            }
            blade?.BeginSwing(Player, Player.GetWeaponDamage(Player.HeldItem), 12, (1 + .5f * charge) * L(1.4f, 3), 3, charge);
        }
        if (hasReturn && !Player.dead && Player.whoAmI == Main.myPlayer) TryTeleport(returnPosition);
        if (same) { var s = Player.GetModPlayer<WeaponPlayer>(); s.SkillActive = false; s.SkillTimer = 0; }
        mode = remaining = duration = 0; activeItem = null; hasReturn = false; SyncAura();
    }
    internal bool TryTeleport(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || position.X < 32 || position.Y < 32
            || position.X + Player.width > Main.maxTilesX * 16 - 32 || position.Y + Player.height > Main.maxTilesY * 16 - 32
            || Collision.SolidCollision(position, Player.width, Player.height)) return false;
        Player.Teleport(position, TeleportationStyleID.RodOfDiscord);
        Player.velocity = Vector2.Zero;
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, position.X, position.Y, TeleportationStyleID.RodOfDiscord);
        return true;
    }
    private void SyncAura()
    {
        foreach (Projectile p in Main.ActiveProjectiles) if (p.owner == Player.whoAmI && p.ModProjectile is CrusherAura) p.netUpdate = true;
    }
    internal void WriteState(BinaryWriter w) { w.Write(mode); w.Write(remaining); w.Write(duration); w.Write(slot); w.Write(level); w.Write(skillItemType); w.Write(Activation); }
    internal void ReadState(BinaryReader r)
    {
        int m = r.ReadInt32(), left = r.ReadInt32(), total = r.ReadInt32(), s = r.ReadInt32(), l = r.ReadInt32(), type = r.ReadInt32(), activation = r.ReadInt32();
        if (Player.whoAmI == Main.myPlayer) return;
        mode = Math.Clamp(m, 0, 3); remaining = Math.Max(0, left); duration = Math.Max(1, total);
        slot = Math.Clamp(s, 0, 57); level = Math.Clamp(l, 1, 10); skillItemType = type; Activation = activation;
    }
    public override void UpdateDead() { mode = remaining = duration = 0; activeItem = null; hasReturn = keyWasDown = false; }
}
