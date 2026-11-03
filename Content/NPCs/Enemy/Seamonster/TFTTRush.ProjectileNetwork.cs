// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Seamonster;

public partial class TFTTRush : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(flytime);
        writer.Write(axis.X); writer.Write(axis.Y);
        writer.Write(unaxis.X); writer.Write(unaxis.Y);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        flytime = reader.ReadInt32();
        axis = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
        unaxis = new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}
