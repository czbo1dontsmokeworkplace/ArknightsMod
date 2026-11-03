// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class IceAltarShockwave : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(_blastDurationTicks);
        writer.Write(_shockwaveRadiusStep);
        writer.Write(_dustMaxLifeTicks);
        writer.Write(_frostAssistBlastBurstSpawned);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        _blastDurationTicks = reader.ReadInt32();
        _shockwaveRadiusStep = reader.ReadSingle();
        _dustMaxLifeTicks = reader.ReadInt32();
        _frostAssistBlastBurstSpawned = reader.ReadBoolean();
    }
}
