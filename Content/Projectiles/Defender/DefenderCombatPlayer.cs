using System;
using ArknightsMod.Players;
using ArknightsMod.Content.Items.Weapons.Defender;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

public sealed class DefenderCombatPlayer : ModPlayer
{
    private int healingCooldown, retaliationCooldown, activeItem, activeSkill = -1, age;
    private int barrier, pendingAbsorb, ammunition, shieldCharges;
    private bool consumed, initialized;
    internal int DefenseStacks { get; set; }
    internal int ResonanceStacks { get; set; }
    internal int RemoteSkill = -1, RemoteTime, RemoteItem;
    internal ulong RemoteUntil;
    internal bool StopsAttacking => DefenderSkillRules.StopsAttacking(Player);
    private WeaponPlayer Skills => Player.GetModPlayer<WeaponPlayer>();
    private string Name => Player.HeldItem.ModItem?.Name;
    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead || Player.CCed || Player.noItems
            || consumed || !DefenderRules.IsWeapon(Player.HeldItem)
            || Skills.CurrentSkill?.Key.Item != Name || Skills.StockCount <= 0 || Skills.SkillActive) return;
        consumed = true;
        Skills.DelStockCount(); Skills.SkillTimer = 0; Skills.SkillActive = true;
        StartSkill();
    }
    private void StartSkill()
    {
        initialized = true; activeItem = Player.HeldItem.type; activeSkill = Skills.Skill; age = 0;
        barrier = ammunition = shieldCharges = DefenseStacks = ResonanceStacks = 0;
        int damage = Player.GetWeaponDamage(Player.HeldItem);
        float duration = Skills.CurrentSkill?.CurrentLevelData.ActiveTime ?? 20f;
        int ticks = Math.Max(24, (int)(duration * 60 * WeaponPlayer.ActiveDurationMultiplier));
        if (Name == "CardiWeapon") { healingCooldown = 0; HealSelf(.4f, 0); }
        if ((Name, activeSkill) is ("LiskarmPistol", 0) or ("AsbestosBlade", 0)) shieldCharges = 1;
        if ((Name, activeSkill) is ("PhilaeBlade", 0) or ("BasslineBlade", 1))
            barrier = (int)(Player.statLifeMax2 * (Name == "PhilaeBlade" ? .45f : .3f));
        if (Name == "CementMaul" && activeSkill == 1) DefenseStacks = 20;
        if (Name == "UnderflowBlade" && activeSkill == 1) DefenseStacks = 5;
        if (Name == "FaithMixer" && activeSkill == 1) ammunition = 50;
        if (Name == "MechanistGun" && activeSkill == 0) ammunition = 3;
        if (Name == "MechanistGun" && activeSkill == 1) { ammunition = 8; barrier = Math.Max(1, Player.statLifeMax2 / 10); }
        if (Name == "AuroraAxe" && activeSkill == 1) ammunition = 9;
        if (Name == "BlitzPistol")
            DefenderSkillArea.Spawn(Player, Player.Center + new Vector2(Player.direction * 70, 0),
                activeSkill == 0 ? 1 : damage * 2, 150, DefenderAreaKind.Electric);
        if (Name == "CementMaul" && activeSkill == 0)
            DefenderSkillArea.Spawn(Player, Player.Center, (int)(damage * 2.8f), 140, DefenderAreaKind.Impact);
        if (Name == "LutonHammer" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center, damage, 190, DefenderAreaKind.Magnet, ticks, 120, true);
        if (Name == "BasslineBlade" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center, damage, 190, DefenderAreaKind.Nervous, Math.Min(ticks, 600), 60, true);
        if (Name == "YuBlade" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center, damage / 2, 180, DefenderAreaKind.Burn);
        if (Name == "SanbyBlade" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center + new Vector2(Player.direction * 100, 10),
                damage / 2, 130, DefenderAreaKind.Glue, ticks, 42);
        if (Name == "HoshigumaShield" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center, (int)(damage * .35f), 95, DefenderAreaKind.Saw, ticks, 15, true);
        if (Name == "HoshigumaBlazingBlade" && activeSkill == 1)
            DefenderSkillArea.Spawn(Player, Player.Center, damage, 115, DefenderAreaKind.Arts, ticks, 30, true);
        if ((Name, activeSkill) is ("CementMaul", 0) or ("BlitzPistol", 0)) Skills.SkillActive = false;
        DefenderSkillVisuals.Burst(Player.Center, DefenderRules.Color(Player.HeldItem), 14);
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .5f }, Player.Center);
        DefenderHoldout.Find(Player)?.SyncSkill();
    }
    internal void HealSelf(float fraction, int cooldown)
    {
        if (Player.whoAmI != Main.myPlayer || healingCooldown > 0) return;
        int amount = Math.Min(Player.statLifeMax2 - Player.statLife, (int)(Player.statLifeMax2 * fraction));
        if (amount <= 0) return;
        Player.Heal(amount); healingCooldown = cooldown;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
    }
    internal void AttackFinished()
    {
        if (!DefenderRules.SkillActive(Player)) return;
        if (DefenderSkillRules.UsesOneAttack(Name, Skills.Skill)) Skills.SkillActive = false;
        if ((Name, Skills.Skill) is ("AuroraAxe", 1) or ("FaithMixer", 1) or ("MechanistGun", 0))
            if (--ammunition <= 0) Skills.SkillActive = false;
    }
    public override void PostUpdate()
    {
        if (healingCooldown > 0) healingCooldown--;
        if (retaliationCooldown > 0) retaliationCooldown--;
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) consumed = false;
        if (Player.whoAmI != Main.myPlayer) { if (RemoteSkill >= 0) RemoteTime++; return; }
        if (!DefenderRules.IsWeapon(Player.HeldItem) || Player.dead || Player.CCed || Player.noItems)
        { Clear(); return; }
        DefenderHoldout.Ensure(Player);
        if (Player.HeldItem.ModItem is Items.Weapons.Defender.Horn.HornGrenadeLauncher) { Clear(); return; }
        if (ArknightsKeybinds.SkillActivatePressed(Player) && Player.HeldItem.ModItem is not Items.Weapons.Defender.Horn.HornGrenadeLauncher) TryActivate();
        bool active = DefenderRules.SkillActive(Player);
        if (active && (!initialized || activeItem != Player.HeldItem.type || activeSkill != Skills.Skill)) StartSkill();
        if (!active)
        {
            if (initialized && activeItem == Player.HeldItem.type && Name == "CzernyBlade" && activeSkill == 1)
                DefenderSkillArea.Spawn(Player, Player.Center,
                    (int)(Player.GetWeaponDamage(Player.HeldItem) * (1f + ResonanceStacks * .15f) * 2.5f), 210, DefenderAreaKind.Arts);
            Clear(); return;
        }
        age++;
        // The armed single-hit skills expire only when an attack is actually made; no permanent stat carryover.
        if (Name == "HeavyrainBlade")
        {
            if (age % 60 == 1 && activeSkill == 0 && EmergencyTarget(Player) == Player) HealSelf(.04f, 0);
        }
        if (Name == "ShalemBlade" && activeSkill == 1 && age % 60 == 0)
        {
            Player.statLife = Math.Max(1, Player.statLife - Math.Max(1, Player.statLifeMax2 / 50));
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
        }
    }
    private void Clear()
    {
        initialized = false; barrier = ammunition = shieldCharges = DefenseStacks = ResonanceStacks = 0;
        activeItem = 0; activeSkill = -1; age = 0;
    }
    public override void UpdateDead() { Clear(); RemoteSkill = -1; RemoteTime = 0; }
    public override void UpdateEquips()
    {
        if (!DefenderRules.SkillActive(Player)) return;
        var stats = DefenderSkillRules.Stats(Player);
        Player.statDefense *= stats.Defense;
        Player.statLifeMax2 = (int)(Player.statLifeMax2 * stats.Life);
        Player.lifeRegen += (int)(Player.statLifeMax2 * stats.Regen * 2f);
        if ((Name, DefenderRules.SkillIndex(Player)) is ("BisonShield", 1) or ("BubbleMaul", 1) or ("YuBlade", 0)) Player.aggro += 600;
        if (Name == "CzernyBlade" && DefenderRules.Skill(Player, 1)) Player.aggro += 600;
        if (Name == "HeavyrainBlade" && (DefenderRules.Skill(Player, 1) || EmergencyTarget(Player) == Player)) { Player.aggro -= 1000; Player.invis = true; }
        if (Name == "AuroraAxe" && DefenderRules.Skill(Player, 0)) { Player.noKnockback = true; Player.buffImmune[BuffID.Chilled] = true; Player.buffImmune[BuffID.Frozen] = true; }
    }
    public override void PostUpdateEquips()
    {
        float defense = 1f;
        foreach (Player ally in Main.ActivePlayers)
        {
            if (ally.whoAmI == Player.whoAmI || ally.dead || !Friendly(Player, ally) || ally.DistanceSQ(Player.Center) > 480f * 480f) continue;
            string name = ally.HeldItem.ModItem?.Name;
            if (name == "NianWeapon" && DefenderRules.Skill(ally, 2)) defense = Math.Max(defense, 1.8f);
            if (name == "BisonShield" && DefenderRules.Skill(ally, 1)) defense = Math.Max(defense, 1.3f);
            if (name == "HeavyrainBlade" && DefenderRules.SkillActive(ally)
                && (DefenderRules.Skill(ally, 1) || EmergencyTarget(ally) == Player))
            {
                Player.aggro -= 1000; Player.invis = true;
                if (DefenderRules.Skill(ally, 0) && Player.statLife < Player.statLifeMax2 / 2) Player.lifeRegen += 8;
            }
        }
        Player.statDefense *= defense;
    }
    private static Player EmergencyTarget(Player source)
    {
        Player target = null;
        float ratio = .5f;
        foreach (Player candidate in Main.ActivePlayers)
        {
            float life = candidate.statLife / (float)Math.Max(1, candidate.statLifeMax2);
            if (candidate.dead || !Friendly(source, candidate) || candidate.DistanceSQ(source.Center) > 480f * 480f || life >= ratio) continue;
            target = candidate; ratio = life;
        }
        return target;
    }
    public override void PreUpdateMovement()
    {
        if (!DefenderSkillRules.Meditating(Player)) return;
        Player.velocity = Vector2.Zero;
    }
    public override bool ImmuneTo(Terraria.DataStructures.PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        => DefenderSkillRules.Meditating(Player);
    public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
    {
        if (DefenderRules.SkillActive(Player)) modifiers.FinalDamage *= 1f - DefenderSkillRules.Stats(Player).Reduction;
    }
    private static bool Friendly(Player a, Player b) => (!a.hostile && !b.hostile) || (a.team != 0 && a.team == b.team);
    private static bool CombatHit(Player.HurtInfo info) => info.DamageSource != null
        && (info.DamageSource.SourceNPCIndex >= 0 || info.DamageSource.SourceProjectileLocalIndex >= 0);
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        pendingAbsorb = 0;
        if (!DefenderRules.SkillActive(Player)) return;
        if (Player.whoAmI != Main.myPlayer || barrier <= 0) return;
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            if (info.Cancelled || !CombatHit(info) || info.Damage <= barrier) return;
            pendingAbsorb = barrier; info.Damage -= barrier;
        };
    }
    public override bool FreeDodge(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer || !CombatHit(info) || Name != "JessicaLiberatedGun"
            || !DefenderRules.Skill(Player, 1) || Main.rand.NextFloat() >= .5f) return false;
        Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 20);
        DefenderSkillVisuals.Burst(Player.Center, DefenderRules.Color(Player.HeldItem), 5);
        return true;
    }
    public override bool ConsumableDodge(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer || !CombatHit(info) || !DefenderRules.SkillActive(Player)) return false;
        bool dodge = false;
        if (shieldCharges > 0 && (Name != "AsbestosBlade" || info.DamageSource.SourceProjectileLocalIndex >= 0)) { shieldCharges--; dodge = true; }
        else if (barrier > 0 && barrier >= info.Damage && pendingAbsorb == 0) { barrier -= info.Damage; dodge = true; }
        else if (Name == "FaithMixer" && Skills.Skill == 1 && info.Damage >= Player.statLife && ammunition >= 20)
        { ammunition -= 20; dodge = true; if (ammunition <= 0) Skills.SkillActive = false; }
        if (!dodge) return false;
        Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 20);
        DefenderSkillVisuals.Burst(Player.Center, DefenderRules.Color(Player.HeldItem));
        RefreshMechanistBarrier();
        return true;
    }
    private void RefreshMechanistBarrier()
    {
        if (Name != "MechanistGun" || !DefenderRules.Skill(Player, 1) || barrier > 0) return;
        DefenderSkillArea.Spawn(Player, Player.Center, Player.GetWeaponDamage(Player.HeldItem), 150, DefenderAreaKind.Electric);
        if (--ammunition <= 0) Skills.SkillActive = false;
        else barrier = Math.Max(1, Player.statLifeMax2 / 10);
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer) return;
        if (pendingAbsorb > 0) { barrier = Math.Max(0, barrier - pendingAbsorb); pendingAbsorb = 0; RefreshMechanistBarrier(); }
        if (!DefenderRules.IsWeapon(Player.HeldItem)) return;
        if (DefenseStacks > 0) DefenseStacks--;
        if (Name == "CzernyBlade" && DefenderRules.Skill(Player, 1)) ResonanceStacks = Math.Min(10, ResonanceStacks + 1);
        DefenderHoldout.Find(Player)?.SyncSkill();
        if (Name == "LiskarmPistol") Skills.TryAddSP(1);
        bool depleted = Name == "UnderflowBlade" && DefenderRules.Skill(Player, 1) && DefenseStacks <= 0;
        if (!DefenderRules.SkillActive(Player) || retaliationCooldown > 0 || !CombatHit(info))
        { if (depleted) Skills.SkillActive = false; return; }
        var kind = (Name, Skills.Skill) switch
        {
            ("NianWeapon", 1) or ("CzernyBlade", 1) or ("HoshigumaBlazingBlade", 0) => DefenderAreaKind.Arts,
            ("BubbleMaul", 1) => DefenderAreaKind.Impact,
            ("PhilaeBlade", 1) => DefenderAreaKind.Necrosis,
            ("UnderflowBlade", 1) => DefenderAreaKind.Corrosion,
            ("YuBlade", 0) => DefenderAreaKind.Burn,
            _ => (DefenderAreaKind?)null
        };
        if (kind == null) { if (depleted) Skills.SkillActive = false; return; }
        retaliationCooldown = Name == "PhilaeBlade" ? 120 : 30;
        int damage = Name == "BubbleMaul" ? Player.statDefense / 2 : (int)(Player.GetWeaponDamage(Player.HeldItem) * (Name == "NianWeapon" ? .9f : 1f));
        DefenderSkillArea.Spawn(Player, Player.Center, damage, 165, kind.Value);
        if (depleted) Skills.SkillActive = false;
    }
}
