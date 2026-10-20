using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    private void UpdateEscortAmmo(int flight)
    {
        if (Kind is DroneShot.ScoutPulse or DroneShot.ArtsOrb) Projectile.penetrate = 1;
        if (Kind != DroneShot.ArtsOrb || flight < 0) return;
        bool valid = Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead;
        if (MaterialistAntagonizer.Authority && !RocketCommitted &&
            (flight >= 72 || !valid || Vector2.DistanceSquared(Projectile.Center, Main.player[Target].Center) < 110 * 110))
        {
            RocketCommitted = true;
            Projectile.netUpdate = true;
        }
        if (!RocketCommitted && valid && flight is >= 18 and < 72)
        {
            float desired = (Main.player[Target].Center - Projectile.Center).ToRotation();
            float maximumTurn = MathHelper.ToRadians(1);
            float turn = MathHelper.Clamp(MathHelper.WrapAngle(desired - Projectile.velocity.ToRotation()), -maximumTurn, maximumTurn);
            Projectile.velocity = Projectile.velocity.RotatedBy(turn).SafeNormalize(Vector2.UnitY) * MaterialistRules.ArtsOrbSpeed;
            if (MaterialistAntagonizer.Authority && flight % 18 == 0) Projectile.netUpdate = true;
        }
        if (!Main.dedServ)
        {
            // Fran's hardmode condensed-core light only: three short-lived bloom pairs, 70% brightness.
            for (int sample = 0; sample < (MaterialistVisuals.Reduced ? 1 : 3); sample++)
            {
                Vector2 point = Projectile.Center - Projectile.velocity * sample / 3f;
                MaterialistMindBloomParticle.Emit(point, Projectile.velocity, flight, Visibility);
            }
        }
    }
}
