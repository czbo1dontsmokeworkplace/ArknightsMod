using System.IO;
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;
public partial class AACT
{
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(anchor.X); writer.Write(anchor.Y); writer.Write(EncounterToken); }
    public override void ReceiveExtraAI(BinaryReader reader) { anchor = new(reader.ReadSingle(), reader.ReadSingle()); EncounterToken = reader.ReadInt32(); }
}
