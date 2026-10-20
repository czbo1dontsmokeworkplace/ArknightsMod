using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    internal const int RetirementTicks = 36;
    internal int RetirementAge = -1;
    internal bool Retiring => RetirementAge >= 0;
    private bool RetirementDue => Moving && Kind is not (DroneShot.ClusterOrb or DroneShot.MachineRound or DroneShot.DropMissile or DroneShot.CannonShell) &&
        Duration >= 72 && Age >= Delay + Duration - RetirementTicks;
    private bool HarmlessRetirement => Retiring || RetirementDue;
    internal float Visibility => Retiring ? 1 - MaterialistFormation.Smooth(RetirementAge / (float)RetirementTicks) : 1;
    internal void Retire()
    {
        if (Retiring || !Projectile.active) return;
        Projectile.hostile = Projectile.friendly = false;
        Projectile.damage = 0;
        Projectile.tileCollide = false;
        // Disable damage before exposing the retirement state to any visual/movement code.
        RetirementAge = 0;
        Projectile.timeLeft = (RetirementTicks + 5) * (Kind == DroneShot.MachineRound ? MaterialistRules.MachineUpdates : 1);
        // A cancelled dormant shot departs as a harmless tracer, without executing its launch effects.
        if (Moving && Projectile.velocity.LengthSquared() < 1)
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * 3;
        if (MaterialistAntagonizer.Authority) Projectile.netUpdate = true;
    }
    private void UpdateRetirement()
    {
        Projectile.hostile = Projectile.friendly = false;
        Projectile.damage = 0;
        Projectile.tileCollide = false;
        if (Moving)
            Projectile.velocity *= MathHelper.Lerp(1.03f, 1.09f, MathHelper.Clamp(RetirementAge / (float)(RetirementTicks - 1), 0, 1));
        Projectile.rotation = Projectile.velocity.ToRotation();
        RetirementAge++;
        Projectile.alpha = (int)(255 * (1 - Visibility));
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, MaterialistVisuals.AmmoColor(this).ToVector3() * (.15f * Visibility));
        if (RetirementAge >= RetirementTicks) Projectile.Kill();
    }
}
