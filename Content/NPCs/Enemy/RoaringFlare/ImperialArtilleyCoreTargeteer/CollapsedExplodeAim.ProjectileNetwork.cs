// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public partial class CollapsedExplodeAim : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(randt);
        writer.Write(frametimer);
        writer.Write(randvelocity.X); writer.Write(randvelocity.Y);
        writer.Write(randspeed);
        writer.Write(randrotation);
        writer.Write(summonedframes);
        for (int i = 0; i < VelocityABCDxy.GetLength(0); i++) for (int j = 0; j < VelocityABCDxy.GetLength(1); j++) writer.Write(VelocityABCDxy[i,j]);
        writer.Write((ushort)MoveLengthABCD.Length); foreach(var v in MoveLengthABCD) writer.Write(v);
        for (int i = 0; i < RectangleABCDxy.GetLength(0); i++) for (int j = 0; j < RectangleABCDxy.GetLength(1); j++) writer.Write(RectangleABCDxy[i,j]);
        writer.Write(r);
        writer.Write(g);
        writer.Write(b);
        writer.Write(a);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        randt = reader.ReadInt32();
        frametimer = reader.ReadInt32();
        randvelocity = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        randspeed = reader.ReadSingle();
        randrotation = reader.ReadSingle();
        summonedframes = reader.ReadBoolean();
        for (int i = 0; i < VelocityABCDxy.GetLength(0); i++) for (int j = 0; j < VelocityABCDxy.GetLength(1); j++) VelocityABCDxy[i,j] = reader.ReadSingle();
        { int count=reader.ReadUInt16(); if(count>256)throw new System.IO.InvalidDataException(); MoveLengthABCD=new float[count]; for(int i=0;i<count;i++) MoveLengthABCD[i]=reader.ReadSingle(); }
        for (int i = 0; i < RectangleABCDxy.GetLength(0); i++) for (int j = 0; j < RectangleABCDxy.GetLength(1); j++) RectangleABCDxy[i,j] = reader.ReadSingle();
        r = reader.ReadSingle();
        g = reader.ReadSingle();
        b = reader.ReadSingle();
        a = reader.ReadSingle();
    }
}
