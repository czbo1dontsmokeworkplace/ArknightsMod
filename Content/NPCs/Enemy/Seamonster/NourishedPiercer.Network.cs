// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class NourishedPiercer
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(approach);
        writer.Write(attack);
        writer.Write(leave);
        writer.Write(attacktime);
        writer.Write(leavetime);
        writer.Write(jumpCD);
        writer.Write(distance);
        writer.Write(diffX);
        writer.Write(diffY);
        writer.Write(directionchoose);
        writer.Write(angle);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        approach = reader.ReadBoolean();
        attack = reader.ReadBoolean();
        leave = reader.ReadBoolean();
        attacktime = reader.ReadInt32();
        leavetime = reader.ReadInt32();
        jumpCD = reader.ReadSingle();
        distance = reader.ReadSingle();
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
        directionchoose = reader.ReadInt32();
        angle = reader.ReadSingle();
    }
}
