// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ReclamationAlgorithm;

public partial class LeporibeastShorthorn
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(status);
        writer.Write(direction);
        writer.Write(run);
        writer.Write(walk);
        writer.Write(jumpCD);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        status = reader.ReadInt32();
        direction = reader.ReadInt32();
        run = reader.ReadBoolean();
        walk = reader.ReadBoolean();
        jumpCD = reader.ReadSingle();
    }
}
