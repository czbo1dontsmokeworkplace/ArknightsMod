// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.OF.Pmp;

public partial class PmpFireBall : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(spawnPos.X); writer.Write(spawnPos.Y);
        writer.Write(targethittime);
        writer.Write(randomay);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        spawnPos = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        targethittime = reader.ReadSingle();
        randomay = reader.ReadSingle();
    }
}
