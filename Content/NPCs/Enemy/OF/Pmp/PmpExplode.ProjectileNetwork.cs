// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.OF.Pmp;

public partial class PmpExplode : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(shadertimer);
        writer.Write(ammount);
        writer.Write(scalesize);
        writer.Write(wavevelocity);
        writer.Write(distortStrength);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        shadertimer = reader.ReadInt32();
        ammount = reader.ReadInt32();
        scalesize = reader.ReadInt32();
        wavevelocity = reader.ReadInt32();
        distortStrength = reader.ReadSingle();
    }
}
