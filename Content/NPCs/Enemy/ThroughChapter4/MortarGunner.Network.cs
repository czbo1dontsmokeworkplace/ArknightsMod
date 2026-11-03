// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class MortarGunner
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(walk);
        writer.Write(AttackCD);
        writer.Write(attack);
        writer.Write(maxspeed);
        writer.Write(jumpCD);
        writer.Write(directionchoose);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        walk = reader.ReadBoolean();
        AttackCD = reader.ReadInt32();
        attack = reader.ReadBoolean();
        maxspeed = reader.ReadSingle();
        jumpCD = reader.ReadInt32();
        directionchoose = reader.ReadInt32();
    }
}
