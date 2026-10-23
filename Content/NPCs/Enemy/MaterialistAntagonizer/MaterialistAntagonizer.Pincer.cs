using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal const int PincerWarningTicks = 96, PincerSweepTicks = 105, PincerDurationTicks = 117;
    internal bool PincerInProgress
    {
        get
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.ModProjectile is MaterialistHazard hazard && hazard.BelongsTo(this) &&
                    hazard.Kind == DroneShot.PincerBeam && !hazard.Retiring) return true;
            return false;
        }
    }
    // A dedicated carrier instrument replaces one quarter of core openings; escorts continue their own roles.
    internal bool CorePincerRound => CoreSequenceActive && CoreAttack == PincerAttack;

    private bool RunPincerBattery(int beat)
    {
        if (!CorePincerRound) return false;
        if (beat != 8) return true;
        int active = 0, ordinary = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is MaterialistHazard hazard && hazard.BelongsTo(this) && !hazard.Retiring)
            {
                active++;
                if (hazard.Kind != DroneShot.MachineRound) ordinary++;
            }
        // Reserve both beams together, including the final-phase ballistic reservation.
        if (active > MaterialistRules.HazardCap - 2 || Phase == 3 && ordinary > MaterialistRules.HazardCap - 74) return true;
        Player target = Main.player[NPC.target];
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 node = CoreMuzzle + new Vector2(side * 82, -12).RotatedBy(NPC.rotation);
            Vector2 delta = target.Center - node;
            Shoot(DroneShot.PincerBeam, node, delta.SafeNormalize(Vector2.UnitY), PincerWarningTicks, PincerDurationTicks,
                length: Math.Max(180, delta.Length()), style: FireStyle.CoreFocus, bend: side);
        }
        return true;
    }
}
