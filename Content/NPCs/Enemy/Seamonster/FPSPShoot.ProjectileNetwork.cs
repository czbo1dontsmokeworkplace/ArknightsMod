// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class FPSPShoot : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(flytime);
        writer.Write(back);
        writer.Write(distance2);
        writer.Write(diffX2);
        writer.Write(diffY2);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        flytime = reader.ReadSingle();
        back = reader.ReadBoolean();
        distance2 = reader.ReadSingle();
        diffX2 = reader.ReadSingle();
        diffY2 = reader.ReadSingle();
    }
}
