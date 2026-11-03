using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Networking;

public interface ILegacyEnemyProjectile { }

public class LegacyProjectileNetwork : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private int snapshotTicks;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is ILegacyEnemyProjectile;
    public override void OnSpawn(Projectile projectile, Terraria.DataStructures.IEntitySource source) => projectile.netImportant = true;
    public override void PostAI(Projectile projectile)
    {
        if (projectile.numUpdates == 0 && Main.netMode == NetmodeID.Server && projectile.owner == Main.myPlayer && ++snapshotTicks >= 30) {
            snapshotTicks = 0;
            projectile.netUpdate = true;
        }
    }
    public static void Write(Projectile p, BinaryWriter writer)
    {
        writer.Write(p.timeLeft);
        for (int i = 0; i < p.localAI.Length; i++) writer.Write(p.localAI[i]);
    }
    public static void Read(Projectile p, BinaryReader reader)
    {
        p.timeLeft = reader.ReadInt32();
        for (int i = 0; i < p.localAI.Length; i++) p.localAI[i] = reader.ReadSingle();
    }
}
