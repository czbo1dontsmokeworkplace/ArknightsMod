// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class IceCrystalAltar
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(shockwaveTimer);
        writer.Write(shockwaveIntervalTicks);
        writer.Write(twinLineRingsSpawned);
        writer.Write(_ambientWispCooldown);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        shockwaveTimer = reader.ReadInt32();
        shockwaveIntervalTicks = reader.ReadInt32();
        twinLineRingsSpawned = reader.ReadBoolean();
        _ambientWispCooldown = reader.ReadInt32();
    }
}
