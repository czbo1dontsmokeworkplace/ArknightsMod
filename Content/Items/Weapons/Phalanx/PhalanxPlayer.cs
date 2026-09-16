using System;
using ArknightsMod.Content.Projectiles.Phalanx;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Weapons.Phalanx;

public sealed class PhalanxPlayer : ModPlayer
{
    public readonly PhalanxShieldState Shield = new();
    private readonly PhalanxCycle cycle = new();
    internal float Opacity;
    internal int FlashSerial;
    internal int ReleaseSerial;
    internal int ChargeFrames => cycle.ChargeFrames;
    private Item chargingItem;
    internal bool Attacking;
    private int immunity, activeSlot = -1;
    private Item activeItem;
    private bool keyConsumed;
    public PhalanxStaff Staff => !Player.dead && !Player.CCed && !Player.noItems
        ? Player.HeldItem.ModItem as PhalanxStaff : null;
    internal int Mode
    {
        get
        {
            WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
            return Staff != null && ReferenceEquals(activeItem, Player.HeldItem)
                && skills.SkillActive && skills.Skill == activeSlot
                && skills.CurrentSkill?.Key.Item == Staff.Name ? activeSlot + 1 : 0;
        }
    }
    public override void SaveData(TagCompound tag)
    {
        tag["PhalanxSpent"] = Shield.Spent;
        tag["PhalanxQuiet"] = Shield.QuietFrames;
        tag["PhalanxRecovery"] = Shield.RecoveryFrames;
    }
    public override void LoadData(TagCompound tag) => Shield.Restore(tag.GetInt("PhalanxSpent"),
        tag.GetInt("PhalanxQuiet"), tag.GetInt("PhalanxRecovery"));

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (immunity > 0) immunity--;
        if (!Player.dead) Shield.Tick();
        bool key = ArknightsKeybinds.SkillActivatePressed(Player);
        if (!key) keyConsumed = false;
        PhalanxStaff staff = Staff;
        if (activeItem != null && (!ReferenceEquals(activeItem, Player.HeldItem)
            || Player.GetModPlayer<WeaponPlayer>().Skill != activeSlot || staff == null))
        {
            var skills = Player.GetModPlayer<WeaponPlayer>();
            if (skills.CurrentSkill?.Key.Item == activeItem.ModItem?.Name) skills.SkillActive = false;
            activeItem = null;
        }
        if (!ReferenceEquals(chargingItem, Player.HeldItem)) cycle.CancelCharge();
        chargingItem = staff == null ? null : Player.HeldItem;
        Attacking = staff != null && Player.controlUseItem && !key && !Player.mouseInterface;
        bool pulse = cycle.Tick(staff != null, Attacking, staff != null && Shield.Remaining(staff.Tier) > 0);
        Opacity = cycle.Opacity;
        if (staff == null) return;
        if (key && !keyConsumed) { keyConsumed = true; Activate(staff); }
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<PhalanxFocus>()] == 0)
            Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<PhalanxFocus>(), 0, 0, Player.whoAmI, staff.Tier);
        if (pulse)
        {
            // Always use game frames: attack speed and repeated clicks cannot compress this interval.
            if (Player.CheckMana(Player.HeldItem, -1, true))
            {
                Player.manaRegenDelay = (int)Player.maxRegenDelay;
                ReleaseSerial++;
                EmitWave(staff, false, true);
            }
        }
    }
    private void Activate(PhalanxStaff staff)
    {
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != staff.Name || skills.SkillActive || skills.StockCount <= 0
            || skills.Skill is < 0 or > 1) return;
        activeItem = Player.HeldItem;
        activeSlot = skills.Skill;
        skills.DelStockCount(); skills.SkillActive = true; skills.SkillTimer = 0;
        FlashSerial++;
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .7f, Pitch = staff.Tier * .15f }, Player.Center);
        if (staff.Tier == 0 && Mode == 2)
        {
            Vector2 offset = Main.MouseWorld - Player.Center;
            offset = offset.SafeNormalize(Vector2.UnitX) * Math.Min(offset.Length(), 360);
            Vector2 position = Player.Center;
            for (int d = 16; d < offset.Length(); d += 16)
            {
                Vector2 next = Player.Center + offset.SafeNormalize(Vector2.UnitX) * d;
                if (Collision.SolidCollision(next - new Vector2(14, 40), 28, 80)
                    || !Collision.CanHitLine(Player.Center, 1, 1, next, 1, 1)) break;
                position = next;
            }
            Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), position, Vector2.Zero,
                ModContent.ProjectileType<BeeswaxObelisk>(), Player.GetWeaponDamage(Player.HeldItem),
                6f, Player.whoAmI);
        }
    }
    // Runs before damage calculation, so even lethal and non-dodgeable hits can be absorbed.
    // Hurt is locally authoritative in Terraria; the focus synchronizes flashes/layers to spectators.
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead) return false;
        if (immunity > 0) return true;
        PhalanxStaff staff = Staff;
        if (staff == null || Opacity < .08f || !Shield.Block(staff.Tier)) return false;
        immunity = 20;
        Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 20);
        for (int i = 0; i < Player.hurtCooldowns.Length; i++)
            Player.hurtCooldowns[i] = Math.Max(Player.hurtCooldowns[i], 20);
        FlashSerial++;
        SoundEngine.PlaySound(SoundID.Item27 with { Volume = .65f, Pitch = staff.Tier * .1f }, Player.Center);
        if (Shield.Remaining(staff.Tier) == 0) EmitWave(staff, true);
        else if (staff.Tier == 2 && Mode == 1) EmitWave(staff, false);
        return true;
    }
    public override void OnHurt(Player.HurtInfo info) => Shield.Disturb();
    internal void EmitWave(PhalanxStaff staff, bool broken, bool chargedAttack = false)
    {
        float multiplier = broken ? 2.5f : 1f;
        if (Mode == 1 && staff.Tier < 2) multiplier *= staff.Tier == 0 ? 1.25f : 1.4f;
        if (Mode == 2 && staff.Tier == 1)
        {
            float seconds = Player.GetModPlayer<WeaponPlayer>().SkillTimer / 60f;
            multiplier *= 1.4f + Math.Min(seconds / 8f, 1f) * 1.2f;
        }
        int style = broken ? 10 : Mode;
        int id = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero,
            ModContent.ProjectileType<PhalanxWave>(), (int)(Player.GetWeaponDamage(Player.HeldItem) * multiplier),
            broken ? 9f : 4f, Player.whoAmI, staff.Tier, style, chargedAttack ? 1 : 0);
        if (Main.projectile.IndexInRange(id)) Main.projectile[id].CritChance = Player.GetWeaponCrit(Player.HeldItem);
        if (staff.Tier == 2 && (Mode == 2 || broken))
        {
            for (int i = 0; i < 12; i++)
                Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center,
                    (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 10f,
                    ModContent.ProjectileType<LinGlassShard>(), Player.GetWeaponDamage(Player.HeldItem) / 3,
                    2f, Player.whoAmI);
        }
    }
    public override void UpdateDead()
    {
        cycle.ClearVisual(); chargingItem = null; Opacity = 0; Attacking = false; activeItem = null; immunity = 0;
        // Death, switching and relogging never forgive shield debt.
    }
}
