// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FNBanFloor : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(GateScaleX);
        writer.Write(GateScaleY);
        writer.Write(GateOpacity);
        writer.Write(MaskOpacity);
        writer.Write(ProjOpacity);
        writer.Write(Projscale);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        GateScaleX = reader.ReadSingle();
        GateScaleY = reader.ReadSingle();
        GateOpacity = reader.ReadSingle();
        MaskOpacity = reader.ReadSingle();
        ProjOpacity = reader.ReadSingle();
        Projscale = reader.ReadSingle();
    }
}
