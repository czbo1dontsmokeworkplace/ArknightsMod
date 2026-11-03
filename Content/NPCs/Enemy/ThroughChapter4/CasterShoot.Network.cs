// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class CasterShoot
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(speedPos.X); writer.Write(speedPos.Y);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        speedPos = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}
