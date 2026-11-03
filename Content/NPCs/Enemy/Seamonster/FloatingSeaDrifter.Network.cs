// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class FloatingSeaDrifter
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(inattack);
        writer.Write(flytime);
        writer.Write(attacktime);
        writer.Write(distance);
        writer.Write(diffX);
        writer.Write(diffY);
        writer.Write(ax);
        writer.Write(directionchoose);
        writer.Write(angle);
        writer.Write(moving);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        inattack = reader.ReadBoolean();
        flytime = reader.ReadSingle();
        attacktime = reader.ReadSingle();
        distance = reader.ReadSingle();
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
        ax = reader.ReadSingle();
        directionchoose = reader.ReadInt32();
        angle = reader.ReadSingle();
        moving = reader.ReadInt32();
    }
}
