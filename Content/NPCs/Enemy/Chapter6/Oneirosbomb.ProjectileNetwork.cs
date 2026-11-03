// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6;

public partial class Oneirosbomb : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(targetPosition.HasValue); if (targetPosition.HasValue) { writer.Write(targetPosition.Value.X); writer.Write(targetPosition.Value.Y); }
        writer.Write(hoverTimer);
        writer.Write(hasLaunched);
        writer.Write(exploded);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        targetPosition = reader.ReadBoolean() ? new Microsoft.Xna.Framework.Vector2(reader.ReadSingle(), reader.ReadSingle()) : null;
        hoverTimer = reader.ReadSingle();
        hasLaunched = reader.ReadBoolean();
        exploded = reader.ReadBoolean();
    }
}
