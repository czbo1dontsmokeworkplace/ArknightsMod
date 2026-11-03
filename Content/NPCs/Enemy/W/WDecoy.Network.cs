// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.W;

public partial class WDecoy
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
    }
}
