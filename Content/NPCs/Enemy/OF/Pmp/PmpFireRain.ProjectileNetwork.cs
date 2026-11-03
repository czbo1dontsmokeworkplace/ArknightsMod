// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.OF.Pmp;

public partial class PmpFireRain : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(vx);
        writer.Write(vy);
        writer.Write(shakeOpacity);
        writer.Write(shakeT);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        vx = reader.ReadSingle();
        vy = reader.ReadSingle();
        shakeOpacity = reader.ReadSingle();
        shakeT = reader.ReadSingle();
    }
}
