using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private int raptorSupportClock;

    private MaterialistWing FiringRaptorPartner(NPC npc, MaterialistAntagonizer boss)
    {
        if (boss.Phase != 3) return null;
        foreach (NPC escort in Main.ActiveNPCs)
            if (escort.whoAmI != npc.whoAmI && escort.TryGetGlobalNPC<MaterialistWing>(out var wing) &&
                wing.BelongsTo(boss) && !wing.Retreating && Role(escort) == WingRole.Raptor && wing.RaptorWindow(boss))
                return wing;
        return null;
    }

    private void FlyRaptorSupport(NPC npc, MaterialistAntagonizer boss)
    {
        Player target = Main.player[boss.NPC.target];
        Vector2 station = target.Center + new Vector2(Slot == 8 ? -430 : 430, -270);
        station = Vector2.Clamp(station, new Vector2(64), new Vector2(Main.maxTilesX * 16 - 64, Main.maxTilesY * 16 - 64));
        MoveRaptorTo(npc, station, 2.6f, .055f);
    }

    private void FireRaptorSupport(NPC npc, MaterialistAntagonizer boss, MaterialistWing partner)
    {
        if (!MaterialistAntagonizer.Authority || partner == null || !boss.EscortsReleased || boss.Paused ||
            !boss.EscortFire || Retreating || raptorLeg == RaptorLeg.Strafe) return;
        int beat = ++raptorSupportClock % 240;
        // Six twin launches per four-second support cycle. Guns still belong exclusively to the active aircraft.
        if (beat < 24 || beat > 144 || beat % 24 != 0) return;
        int active = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(boss) && h.Kind == DroneShot.RaptorRocket && !h.Retiring)
                active++;
        if (active > 10) return;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 port = npc.Center + new Vector2(side * 30, -5).RotatedBy(npc.rotation);
            boss.Shoot(DroneShot.RaptorRocket, port, new Vector2(side * 4.2f, -4.8f),
                duration: 180, shooter: npc.whoAmI, style: FireStyle.Suppression);
        }
    }
}
