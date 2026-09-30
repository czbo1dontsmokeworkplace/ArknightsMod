using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.Evolution;

public sealed class EvolutionOrigin : ModItem
{
    public override string Texture => EvolutionVisuals.Root + "OriginIcon";
    public override void SetDefaults()
    { Item.width = 40; Item.height = 40; Item.accessory = true; Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 5); }
    public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<EvolutionAccessoryPlayer>().OriginEquipped = true;
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        if (!Main.gameMenu) tooltips.Add(new TooltipLine(Mod, "EvolutionStacks", Language.GetTextValue("Mods.ArknightsMod.EvolutionText.Stacks", Main.LocalPlayer.GetModPlayer<EvolutionAccessoryPlayer>().Stacks)));
    }
}
public sealed class EvolutionTerminus : ModItem
{
    public override string Texture => EvolutionVisuals.Root + "PuppetCoreIcon";
    public override void SetDefaults()
    { Item.width = 40; Item.height = 40; Item.accessory = true; Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 5); }
    public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<EvolutionAccessoryPlayer>().TerminusEquipped = true;
}

public sealed class EvolutionAccessoryPlayer : ModPlayer
{
    internal bool OriginEquipped, TerminusEquipped, ForcingDeath;
    internal int Stacks, IdleTicks, DyingTicks;
    private bool deathDebt;
    public override void ResetEffects() { OriginEquipped = false; TerminusEquipped = false; }
    public override void PostUpdateEquips()
    {
        if (!OriginEquipped && Stacks != 0) { Stacks = IdleTicks = 0; if (Main.netMode != NetmodeID.MultiplayerClient) SyncState(); }
        if (OriginEquipped)
        {
            Player.statDefense += 6 + (int)(Stacks * .4f);
            Player.statLifeMax2 += 40; Player.endurance += .05f;
            Player.GetDamage(DamageClass.Generic) += Stacks * .0025f;
            Player.GetCritChance(DamageClass.Generic) += Stacks * .15f;
            Player.moveSpeed += Stacks * .002f;
        }
        // A triggered final form survives unequipping, including its base offensive bonuses.
        if (TerminusEquipped || DyingTicks > 0)
        {
            Player.GetDamage(DamageClass.Generic) += .10f;
            Player.GetCritChance(DamageClass.Generic) += 8;
            Player.GetAttackSpeed(DamageClass.Generic) += .08f;
            Player.moveSpeed += .10f;
        }
        if (DyingTicks > 0)
        {
            Player.GetDamage(DamageClass.Generic) += .50f;
            Player.GetCritChance(DamageClass.Generic) += 30;
            Player.GetAttackSpeed(DamageClass.Generic) += .35f;
            Player.moveSpeed += .25f;
            HoldLastLife();
        }
    }
    public override void PreUpdate() { if (DyingTicks > 0) HoldLastLife(); }
    public override void UpdateLifeRegen() { if (DyingTicks > 0) HoldLastLife(); }
    public override void GetHealLife(Item item, bool quickHeal, ref int healValue) { if (DyingTicks > 0) healValue = 0; }
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => DyingTicks > 0;
    internal void HoldLastLife()
    {
        Player.statLife = 1; Player.lifeRegen = Player.lifeRegenCount = 0; Player.lifeRegenTime = 0;
        Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 2);
        for (int i = 0; i < Player.hurtCooldowns.Length; i++) Player.hurtCooldowns[i] = Math.Max(Player.hurtCooldowns[i], 2);
    }
    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {
        if (ForcingDeath) return true;
        if (DyingTicks > 0) { HoldLastLife(); return false; }
        if (!TerminusEquipped || deathDebt || Player.dead) return true;
        BeginDying();
        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
        { var packet = Mod.GetPacket(); packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.EvolutionAccessory); packet.Write((byte)1); packet.Send(); }
        else if (Main.netMode == NetmodeID.Server) SyncState();
        return false;
    }
    internal void BeginDying()
    {
        if (DyingTicks > 0 || deathDebt) return;
        DyingTicks = 600; deathDebt = true; HoldLastLife();
        if (!Main.dedServ) EvolutionImpactSystem.Emit(Player.Center, 420, 5);
    }
    public override void PostUpdate()
    {
        if (Player.dead) return;
        if (OriginEquipped && Stacks > 0)
        {
            IdleTicks++;
            if (IdleTicks > 1200 && (IdleTicks - 1200) % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            { Stacks--; SyncState(); }
        }
        if (DyingTicks > 0)
        {
            HoldLastLife();
            Player.AddBuff(ModContent.BuffType<EvolutionDyingBuff>(), DyingTicks, quiet: true);
            DyingTicks--;
            if (!Main.dedServ)
            {
                Lighting.AddLight(Player.Center, .65f, .025f, .08f);
                if (DyingTicks % 30 == 0) EvolutionImpactSystem.Emit(Player.Center, 110, 0, true, 25);
            }
        }
        if (deathDebt && DyingTicks <= 0 && (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI == Main.myPlayer))
        {
            ForcingDeath = true;
            bool godMode = Player.creativeGodMode;
            try
            {
                Player.creativeGodMode = false;
                Player.immune = false; Player.immuneTime = 0; Player.statLife = 0;
                var reason = PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.ArknightsMod.EvolutionText.FinalDeath", Player.name));
                Player.KillMe(reason, EvolutionAccessorySystem.TerminalDamage, 0);
                if (Main.netMode == NetmodeID.Server && Player.dead) NetMessage.SendPlayerDeath(Player.whoAmI, reason, EvolutionAccessorySystem.TerminalDamage, 0, false);
            }
            finally { Player.creativeGodMode = godMode; ForcingDeath = false; }
        }
    }
    public override void UpdateDead() { Stacks = IdleTicks = DyingTicks = 0; deathDebt = false; }
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    { UpdateDead(); if (Main.netMode != NetmodeID.MultiplayerClient) SyncState(); }
    public override void SaveData(TagCompound tag) { if (deathDebt) tag["FinalEvolutionDebt"] = 1; }
    public override void LoadData(TagCompound tag) { deathDebt = tag.GetInt("FinalEvolutionDebt") != 0; DyingTicks = 0; }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) { if (Main.netMode == NetmodeID.Server) SyncState(toWho); }
    internal void AwardKill()
    {
        if (!OriginEquipped || Player.dead || EvolutionAccessorySystem.MajorBossAlive()) return;
        Stacks = Math.Min(25, Stacks + 1); IdleTicks = 0; SyncState();
    }
    private void SyncState(int toWho = -1)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Mod.GetPacket(); packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.EvolutionAccessory); packet.Write((byte)0);
        packet.Write((byte)Player.whoAmI); packet.Write((byte)Stacks); packet.Write(IdleTicks); packet.Write((short)DyingTicks); packet.Write(deathDebt); packet.Send(toWho);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        byte kind = reader.ReadByte();
        if (kind == 1 && Main.netMode == NetmodeID.Server && (uint)sender < Main.maxPlayers)
        {
            Player player = Main.player[sender];
            var state = player.GetModPlayer<EvolutionAccessoryPlayer>();
            if (!player.active || player.dead || !state.TerminusEquipped || state.deathDebt) return;
            state.BeginDying(); state.SyncState();
        }
        else if (kind == 0 && Main.netMode == NetmodeID.MultiplayerClient)
        {
            int who = reader.ReadByte(); int stacks = reader.ReadByte(), idle = reader.ReadInt32(), dying = reader.ReadInt16(); bool debt = reader.ReadBoolean();
            if ((uint)who >= Main.maxPlayers) return;
            var state = Main.player[who].GetModPlayer<EvolutionAccessoryPlayer>();
            state.Stacks = Math.Clamp(stacks, 0, 25); state.IdleTicks = Math.Max(0, idle);
            // Receiving our own acknowledgement must not restart the ten-second countdown.
            if (who != Main.myPlayer || debt || Main.player[who].dead || !state.deathDebt)
            {
                state.DyingTicks = state.deathDebt && who == Main.myPlayer ? Math.Min(state.DyingTicks, dying) : Math.Clamp(dying, 0, 600);
                state.deathDebt = debt;
            }
        }
    }
}

public sealed class EvolutionDyingBuff : ModBuff
{
    public override string Texture => EvolutionVisuals.Root + "CompanionBuffIcon";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
}
public sealed class EvolutionKillCredit : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.friendly || npc.boss || npc.lifeMax <= 5 || npc.SpawnedFromStatue || npc.type == NPCID.TargetDummy || npc.realLife >= 0 && npc.realLife != npc.whoAmI) return;
        if ((uint)npc.lastInteraction < Main.maxPlayers && Main.player[npc.lastInteraction].active)
            Main.player[npc.lastInteraction].GetModPlayer<EvolutionAccessoryPlayer>().AwardKill();
    }
}
public sealed class EvolutionAccessorySystem : ModSystem
{
    // Vanilla death packets serialize damage as Int16, even though KillMe accepts double.
    internal const int TerminalDamage = 30000;
    private delegate bool PreKillOriginal(Player player, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason);
    private delegate bool PreKillHook(PreKillOriginal original, Player player, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason);
    private delegate void HealOriginal(Player player, int amount);
    private delegate void HealHook(HealOriginal original, Player player, int amount);
    private delegate void PostUpdateOriginal(Player player);
    private delegate void PostUpdateHook(PostUpdateOriginal original, Player player);
    public override void Load()
    {
        // The hook is automatically removed with this mod by tModLoader. Only the expired debt bypasses resurrection vetoes.
        MonoModHooks.Add(typeof(PlayerLoader).GetMethod(nameof(PlayerLoader.PreKill)), (PreKillHook)EnforceFinalDeath);
        MonoModHooks.Add(typeof(Player).GetMethod(nameof(Player.Heal)), (HealHook)PreventHealing);
        MonoModHooks.Add(typeof(PlayerLoader).GetMethod(nameof(PlayerLoader.PostUpdate)), (PostUpdateHook)ClampLastLife);
    }
    private static void PreventHealing(HealOriginal original, Player player, int amount)
    { if (player.GetModPlayer<EvolutionAccessoryPlayer>().DyingTicks <= 0) original(player, amount); }
    private static void ClampLastLife(PostUpdateOriginal original, Player player)
    {
        original(player);
        var state = player.GetModPlayer<EvolutionAccessoryPlayer>();
        if (state.DyingTicks > 0 && !player.dead) state.HoldLastLife();
    }
    private static bool EnforceFinalDeath(PreKillOriginal original, Player player, double damage, int direction, bool pvp, ref bool sound, ref bool gore, ref PlayerDeathReason reason)
    {
        // Death packets may arrive before a remote countdown expires, or just after its state reset.
        // Recognize our exact terminal death on every peer instead of intercepting it as a new proc.
        if (player.GetModPlayer<EvolutionAccessoryPlayer>().ForcingDeath || IsTerminalDeath(player, damage, reason)) return true;
        return original(player, damage, direction, pvp, ref sound, ref gore, ref reason);
    }
    internal static bool IsTerminalDeath(Player player, double damage, PlayerDeathReason reason) => damage == TerminalDamage &&
        reason.CustomReason != null && reason.CustomReason.ToString() == Language.GetTextValue("Mods.ArknightsMod.EvolutionText.FinalDeath", player.name);
    internal static bool IsMajorBoss(NPC npc) => npc.active && npc.boss && npc.type is not (
        NPCID.MourningWood or NPCID.Pumpking or NPCID.Everscream or NPCID.SantaNK1 or NPCID.IceQueen or
        NPCID.DD2DarkMageT1 or NPCID.DD2DarkMageT3 or NPCID.DD2OgreT2 or NPCID.DD2OgreT3 or NPCID.DD2Betsy or
        NPCID.MartianSaucerCore or NPCID.PirateShip);
    internal static bool MajorBossAlive()
    { foreach (NPC npc in Main.ActiveNPCs) if (IsMajorBoss(npc)) return true; return false; }
}
