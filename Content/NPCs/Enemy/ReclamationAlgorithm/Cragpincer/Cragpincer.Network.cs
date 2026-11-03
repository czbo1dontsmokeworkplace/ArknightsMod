// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.ReclamationAlgorithm.Cragpincer;

public partial class Cragpincer
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(_danceCount);
        writer.Write(_time2);
        writer.Write(_fra);
        writer.Write(_hitting);
        writer.Write(_hateTime);
        writer.Write(_danceName ?? string.Empty);
        writer.Write((int)_state);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        _danceCount = reader.ReadInt32();
        _time2 = reader.ReadInt32();
        _fra = reader.ReadInt32();
        _hitting = reader.ReadBoolean();
        _hateTime = reader.ReadInt32();
        _danceName = reader.ReadString();
        _state = (State)reader.ReadInt32();
    }
}
