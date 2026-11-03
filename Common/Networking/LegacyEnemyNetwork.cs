using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Networking;

/// <summary>
/// Authority and bounded state snapshots for the older enemy implementations.
/// Movement between snapshots uses Terraria's normal velocity/collision prediction.
/// Newer enemies with their own state protocols do not inherit this class.
/// </summary>
public abstract class LegacyEnemyNetwork : ModNPC
{
    private int snapshotTicks;
    protected virtual bool PredictClientAI => false;

    public override bool PreAI()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) {
            if (!NPC.HasValidTarget) NPC.TargetClosest(false);
            return true;
        }
        TickClientPresentation();
        return PredictClientAI;
    }

    // Client-owned hit immunity is separate from the server's attack clocks.
    protected virtual void TickClientPresentation() { }

    public override void PostAI()
    {
        // Also refresh quiet enemies: a new observer must receive the same private
        // animation/attack state as players who witnessed the original spawn.
        if (Main.netMode == NetmodeID.Server && ++snapshotTicks >= 6) {
            snapshotTicks = 0;
            // Non-boss vanilla NPCs normally accrue 30 spam credits per update,
            // limiting snapshots to 2 Hz. This protocol has its own 10 Hz cap;
            // keep that vanilla debt bounded so clients do not predict half a
            // second of an obsolete jump or attack velocity.
            if (!NPC.boss) NPC.netSpam = System.Math.Min(NPC.netSpam, 60);
            NPC.netUpdate = true;
        }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(NPC.noGravity); writer.Write(NPC.noTileCollide);
        writer.Write(NPC.dontTakeDamage); writer.Write(NPC.damage); writer.Write(NPC.defense);
        writer.Write(NPC.alpha); writer.Write(NPC.rotation); writer.Write(NPC.scale);
        writer.Write(NPC.spriteDirection); writer.Write(NPC.color.PackedValue);
        writer.Write(NPC.friendly); writer.Write(NPC.gfxOffY);
        for (int i = 0; i < NPC.localAI.Length; i++) writer.Write(NPC.localAI[i]);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        NPC.noGravity = reader.ReadBoolean(); NPC.noTileCollide = reader.ReadBoolean();
        NPC.dontTakeDamage = reader.ReadBoolean(); NPC.damage = reader.ReadInt32(); NPC.defense = reader.ReadInt32();
        NPC.alpha = reader.ReadInt32(); NPC.rotation = reader.ReadSingle(); NPC.scale = reader.ReadSingle();
        NPC.spriteDirection = reader.ReadInt32(); NPC.color = new Color { PackedValue = reader.ReadUInt32() };
        NPC.friendly = reader.ReadBoolean(); NPC.gfxOffY = reader.ReadSingle();
        for (int i = 0; i < NPC.localAI.Length; i++) NPC.localAI[i] = reader.ReadSingle();
    }
}
