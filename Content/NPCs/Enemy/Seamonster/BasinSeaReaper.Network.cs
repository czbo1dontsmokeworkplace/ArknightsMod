// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class BasinSeaReaper
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(awake);
        writer.Write(sleep);
        writer.Write(jumpCD);
        writer.Write(status);
        writer.Write(direction);
        writer.Write(blooding);
        writer.Write(acceleration);
        writer.Write(maxSpeed);
        writer.Write(waketime);
        writer.Write(distance);
        writer.Write(diffX);
        writer.Write(diffY);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        awake = reader.ReadBoolean();
        sleep = reader.ReadBoolean();
        jumpCD = reader.ReadSingle();
        status = reader.ReadInt32();
        direction = reader.ReadInt32();
        blooding = reader.ReadSingle();
        acceleration = reader.ReadSingle();
        maxSpeed = reader.ReadSingle();
        waketime = reader.ReadSingle();
        distance = reader.ReadSingle();
        diffX = reader.ReadSingle();
        diffY = reader.ReadSingle();
    }
}
