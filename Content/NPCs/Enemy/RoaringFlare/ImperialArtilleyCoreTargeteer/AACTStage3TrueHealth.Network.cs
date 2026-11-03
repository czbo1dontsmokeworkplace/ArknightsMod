// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public partial class AACTStage3TrueHealth
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(usehealth);
        writer.Write(cooldown);
        writer.Write(iscooldownsoundplayed);
        writer.Write(timer);
        writer.Write(randt);
        writer.Write(deathtimer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        usehealth = reader.ReadInt32();
        cooldown = reader.ReadInt32();
        iscooldownsoundplayed = reader.ReadBoolean();
        timer = reader.ReadInt32();
        randt = reader.ReadInt32();
        deathtimer = reader.ReadInt32();
    }
}
