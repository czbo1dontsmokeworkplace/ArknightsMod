using System;
using System.IO;
using ArknightsMod.Content.Items.Armor.Guard.Oblivionis;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay;

// One meter per world/session, never one drain per band member.
internal sealed class MujicaFeverMeter
{
    internal const int Maximum = 450, Duration = 20 * 60;
    internal int Charge { get; private set; }
    internal int Remaining { get; private set; }
    internal bool Active => Remaining > 0;
    internal float DisplayCharge => Active ? Maximum * Remaining / (float)Duration : Charge;
    internal bool AddHit() {
        if (Active || Charge >= Maximum) return false;
        Charge = Math.Min(Maximum, Charge + 3);
        return true;
    }
    internal bool Activate() {
        if (Active || Charge < Maximum) return false;
        Charge = 0;
        Remaining = Duration;
        return true;
    }
    internal void Tick() { if (Remaining > 0) Remaining--; }
    internal void Set(int charge, int remaining) {
        Charge = Math.Clamp(charge, 0, Maximum);
        Remaining = Math.Clamp(remaining, 0, Duration);
    }
}

public sealed class MujicaFeverSystem : ModSystem
{
    internal static readonly MujicaFeverMeter Meter = new();
    internal static int NoteLayers { get; private set; }
    private static bool dirty;
    private static int syncTimer;
    private static uint revision, receivedRevision;
    private static ulong startedTick;
    internal static bool IsMember(Player player) => player != null && player.active && !player.dead
        && OperatorTagHelper.PlayerHasFaction(player, OperatorFaction.AveMujica);
    internal static bool HasHelmet(Player p) => p != null && p.active && !p.dead
        && p.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisHead>();
    internal static bool HasSet(Player p) => HasHelmet(p)
        && p.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisBody>()
        && p.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisLegs>();
    internal static bool Protected(Player player) => Meter.Active && IsMember(player);
    internal static bool PauseSkill(Player player) => IsMember(player)
        && (Meter.Active || player.GetModPlayer<Players.MujicaPlayer>().WaitingForActivation);

    public override void OnWorldLoad() => Reset();
    public override void OnWorldUnload() => Reset();
    private static void Reset() {
        Meter.Set(0, 0); NoteLayers = 0; dirty = false; syncTimer = 0;
        revision = receivedRevision = 0; startedTick = ulong.MaxValue;
    }
    public override void PreUpdatePlayers() {
        bool source = false;
        foreach (Player p in Main.ActivePlayers) if (HasHelmet(p)) { source = true; break; }
        NoteLayers = source ? Math.Min(10, OperatorTagHelper.CountNotesOnField()) : 0;
    }
    public override void PostUpdateEverything() {
        bool active = Meter.Active;
        if (Main.GameUpdateCount != startedTick) Meter.Tick();
        if (active && !Meter.Active) dirty = true;
        if (Main.netMode != NetmodeID.Server) return;
        if (++syncTimer >= 15 || dirty && Meter.Charge == MujicaFeverMeter.Maximum) {
            syncTimer = 0;
            if (dirty || active) Sync();
        }
    }
    internal static void ReportHit(Player player, NPC target, int damage) {
        if (damage <= 0 || target == null || target.friendly || target.lifeMax <= 5 || !HasSet(player)) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (player.whoAmI != Main.myPlayer) return;
            var packet = Packet(0); packet.Write((short)target.whoAmI); packet.Send();
        }
        else if (Main.netMode == NetmodeID.SinglePlayer) dirty |= Meter.AddHit();
        // Owner reports are authoritative for tML player hit callbacks in multiplayer.
    }
    internal static void SkillStarted(Player player) {
        if (!IsMember(player) || Meter.Active) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            if (player.whoAmI != Main.myPlayer) return;
            // Always notify the authority: the last filling hit may still be in transit.
            if (Meter.Charge >= MujicaFeverMeter.Maximum)
                player.GetModPlayer<Players.MujicaPlayer>().AwaitActivation();
            Packet(1).Send();
        }
        else if (Meter.Activate()) {
            startedTick = Main.GameUpdateCount;
            if (Main.netMode == NetmodeID.Server) Sync();
        }
    }
    private static ModPacket Packet(byte kind) {
        var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
        packet.Write((short)ArknightsMod.ArkMessageID.MujicaFever);
        packet.Write(kind);
        return packet;
    }
    internal static void Sync(int toWho = -1) {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Packet(2);
        packet.Write(++revision);
        packet.Write((ushort)Meter.Charge);
        packet.Write((ushort)Meter.Remaining);
        packet.Send(toWho);
        if (toWho < 0) dirty = false;
    }
    internal static void Receive(BinaryReader reader, int sender) {
        byte kind = reader.ReadByte();
        if (Main.netMode == NetmodeID.Server) {
            if (sender < 0 || sender >= Main.maxPlayers) return;
            Player player = Main.player[sender];
            if (kind == 0) {
                int npcIndex = reader.ReadInt16();
                if (!HasSet(player) || npcIndex < 0 || npcIndex >= Main.maxNPCs) return;
                NPC npc = Main.npc[npcIndex];
                if (npc.friendly || npc.lifeMax <= 5 || npc.dontTakeDamage) return;
                dirty |= Meter.AddHit();
            }
            else if (kind == 1 && IsMember(player)) {
                if (Meter.Activate()) { startedTick = Main.GameUpdateCount; Sync(); }
                else Sync(sender); // Rejection also releases the client's pending pause.
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && kind == 2) {
            uint next = reader.ReadUInt32();
            int charge = reader.ReadUInt16(), remaining = reader.ReadUInt16();
            if (next <= receivedRevision) return;
            receivedRevision = next;
            Meter.Set(charge, remaining);
            if (remaining > 0 || charge < MujicaFeverMeter.Maximum)
                Main.LocalPlayer.GetModPlayer<Players.MujicaPlayer>().ConfirmActivation();
        }
    }
}
