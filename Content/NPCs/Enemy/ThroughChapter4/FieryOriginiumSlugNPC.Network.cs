// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

public partial class FieryOriginiumSlugNPC
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write((int)_state);
        writer.Write(_attackTimer);
        writer.Write(_nextAttackCooldown);
        writer.Write(_status);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        _state = (ActionState)reader.ReadInt32();
        _attackTimer = reader.ReadInt32();
        _nextAttackCooldown = reader.ReadInt32();
        _status = reader.ReadInt32();
    }
}
