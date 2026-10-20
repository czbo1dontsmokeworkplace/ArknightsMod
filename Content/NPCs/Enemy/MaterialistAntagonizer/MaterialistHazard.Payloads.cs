using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    private bool UpdatePayload(MaterialistAntagonizer boss, int flight)
    {
        if (Kind is DroneShot.GravityOrb or DroneShot.DropMissile)
        {
            Projectile.tileCollide = Age >= Delay;
            Projectile.penetrate = 1;
        }
        if (Kind == DroneShot.GravityOrb && flight >= 0) Projectile.velocity.Y += .22f;
        if (Kind == DroneShot.Beacon)
        {
            if (Age < 45) Projectile.velocity *= .92f;
            else Projectile.velocity = Vector2.Zero;
            if (!Main.dedServ && Age == 45)
            {
                MaterialistVisuals.Burst(Projectile.Center, MaterialistVisuals.Cyan, .6f);
                MaterialistEffects.Pulse(Projectile.Center, 50, MaterialistVisuals.Cyan);
            }
            if (Age == 84 && MaterialistAntagonizer.Authority)
            {
                Vector2 marker = Projectile.Center;
                Vector2 start = new(marker.X, Math.Max(48, marker.Y - 880));
                boss.Shoot(DroneShot.DropMissile, start, Vector2.UnitY * 23, 24, 54,
                    style: FireStyle.CoreFocus, bend: Bend, mark: marker);
            }
        }
        if (Kind == DroneShot.DropMissile && flight >= 0 && Bend > .5f && Projectile.Center.Y >= Anchor.Y)
        {
            if (MaterialistAntagonizer.Authority)
            {
                boss.Shoot(DroneShot.Bombardment, Anchor, Vector2.Zero, 0, 18, style: FireStyle.CoreFocus);
                Projectile.Kill();
            }
            return false;
        }
        return true;
    }
}
