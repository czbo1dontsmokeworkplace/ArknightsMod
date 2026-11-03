// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class ShieldGuard
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(attackTimer);
        writer.Write(isAttacking);
        writer.Write(isWalking);
        writer.Write(frameCounter);
        writer.Write(currentFrame);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        attackTimer = reader.ReadInt32();
        isAttacking = reader.ReadBoolean();
        isWalking = reader.ReadBoolean();
        frameCounter = reader.ReadInt32();
        currentFrame = reader.ReadInt32();
    }
}
