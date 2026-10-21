using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal bool CoreSaucerRound => Order == FlightOrder.Command && !CorePincerRound &&
        Math.Abs(Cycle + Wave + Phase + Math.Max(0, (Timer - 60) / 180)) % 4 == 3;
    internal Vector2 SaucerPort(int side) => NPC.Center + new Vector2(side * 70, 7).RotatedBy(NPC.rotation) * 1.45f;

    private bool RunSaucerBattery(int beat)
    {
        if (!CoreSaucerRound) return false;
        if (beat == 8) { CoreSpreadAim = Main.player[NPC.target].Center; NPC.netUpdate = true; }
        if (beat < 48 || beat > 132 || (beat - 48) % 3 != 0) return true;
        int active = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is MaterialistHazard hazard && hazard.BelongsTo(this) &&
                !hazard.Retiring && hazard.Kind == DroneShot.SaucerRound) active++;
        if (active >= 32) return true;
        int shot = (beat - 48) / 3, side = shot % 2 == 0 ? -1 : 1;
        Vector2 muzzle = SaucerPort(side);
        // Wide, stratified random lanes: no repeatedly sampled player aim or fast homing bullets.
        float spread = (((shot * 7) % 13 + Main.rand.NextFloat()) / 13f * 2 - 1) * .85f;
        Vector2 direction = (CoreSpreadAim - muzzle).SafeNormalize(Vector2.UnitY).RotatedBy(spread);
        Shoot(DroneShot.SaucerRound, muzzle, direction * Main.rand.NextFloat(4.1f, 4.5f),
            duration: 156, style: FireStyle.CoreSpread);
        return true;
    }
}
