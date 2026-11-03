using System.IO;
using ArknightsMod.Content.NPCs.Enemy.Seamonster;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Networking;

internal static class SeabornContactSync
{
    private static bool Supported(NPC npc) => npc.ModNPC is DeepSeaSlider or PocketSeaCrawler or ShellSeaRunner;

    internal static void Report(NPC npc, Player victim)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) {
            Apply(npc);
            return;
        }
        if (victim.whoAmI != Main.myPlayer) return;
        var packet = npc.ModNPC.Mod.GetPacket();
        packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.SeabornContact);
        packet.Write((short)npc.whoAmI); packet.Write(npc.type);
        packet.Send();
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        int slot, type;
        try { slot = reader.ReadInt16(); type = reader.ReadInt32(); }
        catch (EndOfStreamException) { return; }
        if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers || (uint)slot >= Main.maxNPCs) return;
        Player player = Main.player[sender]; NPC npc = Main.npc[slot];
        if (!player.active || player.dead || !npc.active || npc.type != type || !Supported(npc)) return;
        var bounds = npc.Hitbox; bounds.Inflate(80, 80);
        if (!bounds.Intersects(player.Hitbox)) return;
        var state = npc.GetGlobalNPC<SeabornContactHistory>();
        state.LastContact ??= new ulong[Main.maxPlayers];
        ulong now = Main.GameUpdateCount;
        if (state.LastContact[sender] != 0 && now - state.LastContact[sender] < 15) return;
        state.LastContact[sender] = now;
        Apply(npc);
    }

    private static void Apply(NPC npc)
    {
        if (!Supported(npc)) return;
        npc.ai[3] = npc.ModNPC is ShellSeaRunner ? 202 : 200;
        npc.netUpdate = true;
    }
}

public class SeabornContactHistory : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal ulong[] LastContact;
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is DeepSeaSlider or PocketSeaCrawler or ShellSeaRunner;
    public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source) => LastContact = new ulong[Main.maxPlayers];
}
