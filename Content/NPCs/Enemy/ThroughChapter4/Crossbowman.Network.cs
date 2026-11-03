// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class Crossbowman
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(timer);
        writer.Write(atkloop);
        writer.Write(atkrange);
        writer.Write(escrange);
        writer.Write(iswalk);
        writer.Write(isatk);
        writer.Write(isescape);
        writer.Write(isstuck);
        writer.Write(jumpstage);
        writer.Write(jumpspeed);
        writer.Write(jumptimes);
        writer.Write(distance);
        writer.Write(diffX);
        writer.Write(diffY);
        writer.Write(ax);
        writer.Write(vx);
        writer.Write(escapetimer);
        writer.Write(escapetime);
        writer.Write(attacktimer);
        writer.Write(angle);
        writer.Write(directionchoose);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        timer = reader.ReadSingle();
        atkloop = reader.ReadInt32();
        atkrange = reader.ReadInt32();
        escrange = reader.ReadInt32();
        iswalk = reader.ReadBoolean();
        isatk = reader.ReadBoolean();
        isescape = reader.ReadBoolean();
        isstuck = reader.ReadBoolean();
        jumpstage = reader.ReadInt32();
        jumpspeed = reader.ReadSingle();
        jumptimes = reader.ReadInt32();
        distance = reader.ReadSingle();
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
        ax = reader.ReadSingle();
        vx = reader.ReadSingle();
        escapetimer = reader.ReadSingle();
        escapetime = reader.ReadSingle();
        attacktimer = reader.ReadInt32();
        angle = reader.ReadSingle();
        directionchoose = reader.ReadInt32();
    }
}
