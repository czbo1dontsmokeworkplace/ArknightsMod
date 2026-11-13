// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class BlackIceProjectile : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(deltatime);
        writer.Write(offsettime);
        writer.Write(flightTicks);
        WriteFormationShard(writer);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        deltatime = reader.ReadSingle();
        offsettime = reader.ReadSingle();
        flightTicks = System.Math.Clamp(reader.ReadInt32(), 0, 600);
        ReadFormationShard(reader);
    }
}
