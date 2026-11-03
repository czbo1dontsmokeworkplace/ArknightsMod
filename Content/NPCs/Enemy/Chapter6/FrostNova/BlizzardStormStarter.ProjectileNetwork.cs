// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class BlizzardStormStarter : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(r);
        writer.Write(g);
        writer.Write(move1X);
        writer.Write(move1Y);
        writer.Write(oldPosX);
        writer.Write(oldPosY);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        r = reader.ReadInt32();
        g = reader.ReadInt32();
        move1X = reader.ReadSingle();
        move1Y = reader.ReadSingle();
        oldPosX = reader.ReadSingle();
        oldPosY = reader.ReadSingle();
    }
}
