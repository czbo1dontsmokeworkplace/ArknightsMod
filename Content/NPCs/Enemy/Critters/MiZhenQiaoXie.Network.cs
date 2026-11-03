// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Critters;

public partial class MiZhenQiaoXie
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(fleeTimer);
        writer.Write(fleeDirection);
        writer.Write(wasFleeing);
        writer.Write(wheelRotation);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        fleeTimer = reader.ReadInt32();
        fleeDirection = reader.ReadInt32();
        wasFleeing = reader.ReadBoolean();
        wheelRotation = reader.ReadSingle();
    }
}
