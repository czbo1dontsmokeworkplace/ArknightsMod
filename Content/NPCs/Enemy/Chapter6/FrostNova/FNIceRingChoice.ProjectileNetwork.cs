// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FNIceRingChoice : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(icenum);
        writer.Write(icenumshooted);
        writer.Write(speedY);
        WriteFormation(writer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        icenum = reader.ReadInt32();
        icenumshooted = reader.ReadInt32();
        speedY = reader.ReadSingle();
        ReadFormation(reader);
    }
}
