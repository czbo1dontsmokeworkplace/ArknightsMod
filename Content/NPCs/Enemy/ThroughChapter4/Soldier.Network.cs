// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class Soldier
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(rushtime);
        writer.Write(attackCD);
        writer.Write(jumpCD);
        writer.Write(walktime);
        writer.Write(attacktime);
        writer.Write(rush);
        writer.Write(walk);
        writer.Write(attack);
        writer.Write(maxspeed1);
        writer.Write(maxspeed2);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        rushtime = reader.ReadInt32();
        attackCD = reader.ReadInt32();
        jumpCD = reader.ReadInt32();
        walktime = reader.ReadInt32();
        attacktime = reader.ReadInt32();
        rush = reader.ReadBoolean();
        walk = reader.ReadBoolean();
        attack = reader.ReadBoolean();
        maxspeed1 = reader.ReadSingle();
        maxspeed2 = reader.ReadSingle();
    }
}
