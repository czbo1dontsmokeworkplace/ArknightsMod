// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6;

internal partial class SnowSniper
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(AttackCD);
        writer.Write(attack);
        writer.Write(walk);
        writer.Write(maxspeed);
        writer.Write(directionchoose);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        AttackCD = reader.ReadInt32();
        attack = reader.ReadBoolean();
        walk = reader.ReadBoolean();
        maxspeed = reader.ReadSingle();
        directionchoose = reader.ReadInt32();
    }
}
