// Explicit wire layout for legacy enemy state. Keep send/receive order identical.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6;

public partial class Oneiros
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        base.SendExtraAI(writer);
        writer.Write(AttackCD);
        writer.Write(attack);
        writer.Write(walk);
        writer.Write(walktimer);
        writer.Write(Targetpos1.X); writer.Write(Targetpos1.Y);
        writer.Write(Targetpos2.X); writer.Write(Targetpos2.Y);
        writer.Write(Vplus.X); writer.Write(Vplus.Y);
        writer.Write(directionchoose);
        writer.Write(Holdtime);
        writer.Write(preattack);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);
        AttackCD = reader.ReadInt32();
        attack = reader.ReadBoolean();
        walk = reader.ReadBoolean();
        walktimer = reader.ReadInt32();
        Targetpos1 = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        Targetpos2 = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        Vplus = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        directionchoose = reader.ReadInt32();
        Holdtime = reader.ReadInt32();
        preattack = reader.ReadBoolean();
    }
}
