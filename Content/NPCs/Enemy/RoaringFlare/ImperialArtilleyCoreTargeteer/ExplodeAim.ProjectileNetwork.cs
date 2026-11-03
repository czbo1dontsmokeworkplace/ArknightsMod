// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public partial class ExplodeAim : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(randomx);
        writer.Write(missled);
        writer.Write(timer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        randomx = reader.ReadSingle();
        missled = reader.ReadBoolean();
        timer = reader.ReadSingle();
    }
}
