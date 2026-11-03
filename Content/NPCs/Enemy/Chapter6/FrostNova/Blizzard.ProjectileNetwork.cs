// Explicit state for server-owned enemy hazards, including observers joining mid-attack.
namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class Blizzard : global::ArknightsMod.Common.Networking.ILegacyEnemyProjectile
{
    public override void SendExtraAI(System.IO.BinaryWriter writer)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Write(Projectile, writer);
        writer.Write(timer);
        writer.Write(addspeedy);
        writer.Write(ScreenMedPlaceDisRate);
        writer.Write(cosspeedy);
        writer.Write(sinspeedx);
        writer.Write(colorstate);
        writer.Write(r);
        writer.Write(g);
    }

    public override void ReceiveExtraAI(System.IO.BinaryReader reader)
    {
        global::ArknightsMod.Common.Networking.LegacyProjectileNetwork.Read(Projectile, reader);
        timer = reader.ReadSingle();
        addspeedy = reader.ReadSingle();
        ScreenMedPlaceDisRate = reader.ReadSingle();
        cosspeedy = reader.ReadSingle();
        sinspeedx = reader.ReadSingle();
        colorstate = reader.ReadSingle();
        r = reader.ReadInt32();
        g = reader.ReadInt32();
    }
}
