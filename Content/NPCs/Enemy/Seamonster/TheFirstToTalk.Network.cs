// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class TheFirstToTalk
{
    protected override bool PredictClientAI => true;
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(walk);
        writer.Write(shoot);
        writer.Write(rush);
        writer.Write(skill);
        writer.Write(leave);
        writer.Write(SP);
        writer.Write(jumpCD);
        writer.Write(shootCD);
        writer.Write(rushCD);
        writer.Write(rushtime);
        writer.Write(shoottime);
        writer.Write(skilltime);
        writer.Write(extime);
        writer.Write(ask);
        writer.Write(rushd.X); writer.Write(rushd.Y);
        writer.Write(oldpos1.X); writer.Write(oldpos1.Y);
        writer.Write(oldpos2.X); writer.Write(oldpos2.Y);
        writer.Write(oldpos3.X); writer.Write(oldpos3.Y);
        writer.Write(texturedirection);
        writer.Write(alpha);
        writer.Write(PWH);
        writer.Write(PWD);
        writer.Write(UPX);
        writer.Write(PWV.X); writer.Write(PWV.Y);
        writer.Write(PWJ);
        writer.Write(G);
        writer.Write(spawntimer);
        writer.Write(spawn);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        walk = reader.ReadBoolean();
        shoot = reader.ReadBoolean();
        rush = reader.ReadBoolean();
        skill = reader.ReadBoolean();
        leave = reader.ReadBoolean();
        SP = reader.ReadInt32();
        jumpCD = reader.ReadInt32();
        shootCD = reader.ReadInt32();
        rushCD = reader.ReadInt32();
        rushtime = reader.ReadInt32();
        shoottime = reader.ReadInt32();
        skilltime = reader.ReadInt32();
        extime = reader.ReadInt32();
        ask = reader.ReadBoolean();
        rushd = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        oldpos1 = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        oldpos2 = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        oldpos3 = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        texturedirection = reader.ReadSingle();
        alpha = reader.ReadSingle();
        PWH = reader.ReadSingle();
        PWD = reader.ReadSingle();
        UPX = reader.ReadSingle();
        PWV = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        PWJ = reader.ReadSingle();
        G = reader.ReadSingle();
        spawntimer = reader.ReadSingle();
        spawn = reader.ReadInt32();
    }
}
