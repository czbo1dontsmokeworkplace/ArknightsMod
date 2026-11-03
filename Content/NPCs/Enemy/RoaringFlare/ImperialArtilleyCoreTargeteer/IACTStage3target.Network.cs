// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public partial class IACTStage3target
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(timer);
        writer.Write(stage3atkloop);
        writer.Write(killtimer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        timer = reader.ReadInt32();
        stage3atkloop = reader.ReadInt32();
        killtimer = reader.ReadInt32();
    }
}
