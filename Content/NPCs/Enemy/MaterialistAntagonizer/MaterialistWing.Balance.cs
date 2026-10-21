using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private ulong nextVolley, currentVolley = ulong.MaxValue;
    private int volleyShots;

    internal bool PrepareShot(NPC npc, MaterialistAntagonizer boss, ref DroneShot kind, ref Vector2 velocity,
        ref int delay, ref int duration, ref float bend)
    {
        if (!boss.EscortsReleased) return false;
        WingRole role = Role(npc);
        if (role == WingRole.Raptor)
        {
            if (kind == DroneShot.RaptorRocket)
            {
                duration = 180;
                return boss.Phase == 3 && !RaptorWindow(boss) && FiringRaptorPartner(npc, boss) != null && boss.EscortFire && !boss.Paused;
            }
            if (kind != DroneShot.MachineRound || !RaptorWindow(boss)) return false;
            velocity = velocity.SafeNormalize(Vector2.UnitY) * 15; // Kingsbane AuricBullet: 15 * 16 updates.
            duration = 24;
            return true;
        }
        if (role == WingRole.ArtsA2 && kind is not (DroneShot.Beam or DroneShot.ChaseBeam)) return false;
        if (role == WingRole.ArtsA1 && kind != DroneShot.ArtsOrb) return false;
        if (role is WingRole.Drone or WingRole.MkII)
        {
            // Ordinary drones never own range-locking beams, rails or persistent area walls.
            if (kind is DroneShot.Beam or DroneShot.ChaseBeam or DroneShot.FluxMine or DroneShot.RailSlug) return false;
            if (kind != DroneShot.TrailBomb)
            {
                kind = DroneShot.ScoutPulse;
                float speed = MathHelper.Clamp(velocity.Length() * .52f, 3, 4.1f);
                velocity = velocity.SafeNormalize(Vector2.UnitY) * speed;
                duration = MaterialistRules.ScoutLifetime;
                bend = 0; // Same visual as Rockrock; deliberately no homing, curvature or endless acceleration.
            }
        }
        ulong now = Main.GameUpdateCount;
        int interval = role == WingRole.ArtsA1 ? 150 : role == WingRole.ArtsA2 ? 160 : role == WingRole.MkII ? 50 : 44;
        bool spinning = !boss.ArtilleryActive && (boss.ScoutReady ? boss.ScoutAttack(Slot) == ScoutMove.SpinRing :
            boss.Pattern is WingPattern.SpinVolley or WingPattern.Spiral or WingPattern.FinalVolley);
        int capacity = role is WingRole.ArtsA1 or WingRole.ArtsA2 ? 1 : spinning ? 2 : 3;
        if (spinning && kind == DroneShot.ScoutPulse) interval = 18;
        if (kind == DroneShot.TrailBomb) { interval = 18; capacity = 1; }
        if (currentVolley != now)
        {
            if (now < nextVolley) return false;
            currentVolley = now; volleyShots = 0;
            nextVolley = now + (ulong)interval;
        }
        return volleyShots++ < capacity;
    }
}
