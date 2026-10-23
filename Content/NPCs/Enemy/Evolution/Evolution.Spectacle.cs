using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    private int spectaclePhase = -1, spectacleSerial = -1, spectacleTimer = -1;
    private bool spectacleCharging;
    private void UpdateBloodSpectacle()
    {
        if (Main.dedServ) return;
        bool newTurn = spectacleSerial != Serial || spectaclePhase != Phase;
        int previous = newTurn ? Timer - 1 : spectacleTimer;
        bool Crossed(int tick) => previous < tick && Timer >= tick && Timer - tick < 5;
        if (spectaclePhase != Phase && spectaclePhase >= 0 && Phase >= 2 && Phase != 6)
            EvolutionBloodSpectacleSystem.Emit(NPC.Center, -Vector2.UnitY,
                Phase == 6 ? EvolutionBloodCue.Rupture : EvolutionBloodCue.Echo, ThemeColor, Phase >= 4 ? 1.8f : 1.3f,
                Phase == 6 ? EvolutionPresentationRules.DeathExpansionScale : 1);
        if (Charging && (!spectacleCharging || newTurn))
            EvolutionBloodSpectacleSystem.Emit(NPC.Center, -DashDirection, EvolutionBloodCue.Dash, ThemeColor, Phase == 5 ? 1.5f : 1.1f);
        if (Charging && Timer % 5 == 0 && Timer != previous)
            EvolutionBloodSpectacleSystem.Emit(NPC.Center - NPC.velocity, -NPC.velocity, EvolutionBloodCue.Volley, ThemeColor, .85f);
        if (Phase == 3 && Attack == 14 && Crossed(EvolutionFleetRules.SlamImpact))
            EvolutionBloodSpectacleSystem.Emit(CrashGround, -Vector2.UnitY, EvolutionBloodCue.Impact, ThemeColor, 1.7f);
        if (Transitioning)
        {
            int time = EvolutionRules.CinematicTime(Phase, Timer);
            int past = EvolutionRules.CinematicTime(Phase, previous);
            if (past < EvolutionCinematicFrame.BurstTime && time >= EvolutionCinematicFrame.BurstTime && time - EvolutionCinematicFrame.BurstTime < 6)
                EvolutionBloodSpectacleSystem.Emit(NPC.Center, -Vector2.UnitY, EvolutionBloodCue.Morph, ThemeColor, Phase == 4 ? 2.2f : 1.8f);
        }
        if (Phase == 5 && Attack == 6)
        {
            int fire = EvolutionRules.AxisSetupTicks + EvolutionRules.FireTime(EvolutionShot.Beam,
                EvolutionRules.ProjectileWarning(EvolutionShot.Beam, EvolutionRules.AxisChargeTicks, true));
            if (Crossed(fire)) EvolutionBloodSpectacleSystem.Emit(NPC.Center, -Vector2.UnitY, EvolutionBloodCue.Axis, ThemeColor, 2, follow: this);
            if (Timer > fire && Timer < fire + EvolutionRules.AxisActiveTicks && (Timer - fire) % 84 == 0 && Timer != previous)
                EvolutionBloodSpectacleSystem.Emit(NPC.Center, -Vector2.UnitY, EvolutionBloodCue.Echo, ThemeColor, 1.15f, follow: this);
        }
        if (Phase == 6)
        {
            if (Crossed(EvolutionPresentationRules.DeathFinale))
                EvolutionBloodSpectacleSystem.Emit(NPC.Center, -Vector2.UnitY, EvolutionBloodCue.Finale, ThemeColor, 2.2f,
                    EvolutionPresentationRules.DeathExpansionScale, density: EvolutionPresentationRules.DeathDensity,
                    opacity: EvolutionPresentationRules.DeathOpacity, intensity: EvolutionPresentationRules.DeathIntensity);
            else if (Timer < EvolutionPresentationRules.DeathFinale && EvolutionPresentationRules.DeathBurst(Timer) && Timer != previous)
                EvolutionBloodSpectacleSystem.Emit(EvolutionPresentationRules.DeathPoint(NPC.Center, Timer, 0),
                    -Vector2.UnitY, EvolutionBloodCue.Rupture, ThemeColor, 1 + Timer / 180f, EvolutionPresentationRules.DeathExpansionScale,
                    density: EvolutionPresentationRules.DeathDensity, opacity: EvolutionPresentationRules.DeathOpacity, intensity: EvolutionPresentationRules.DeathIntensity);
        }
        spectaclePhase = Phase; spectacleSerial = Serial; spectacleTimer = Timer; spectacleCharging = Charging;
    }
}

public sealed partial class EvolutionHazard
{
    private bool spectacleLaunched;
    private void UpdateLaunchSpectacle(Evolution boss)
    {
        if (Main.dedServ || spectacleLaunched || boss.Phase < 2 || Age < FireAge) return;
        spectacleLaunched = true;
        if (Age - FireAge > 3 || IsAxis) return; // Late joins never replay old explosions.
        if (Kind is EvolutionShot.Beam or EvolutionShot.Lance or EvolutionShot.CrashFan or EvolutionShot.CrimsonBomb)
        {
            EvolutionBloodSpectacleSystem.Emit(Projectile.Center, Projectile.velocity,
                Kind == EvolutionShot.CrashFan ? EvolutionBloodCue.Impact : EvolutionBloodCue.Volley, boss.ThemeColor,
                Kind == EvolutionShot.CrashFan ? 1.5f : .8f);
            if (BeamStyle == EvolutionBeamStyle.Refracted && Kind == EvolutionShot.Beam)
            {
                EvolutionRayPath path = RayPath(Projectile.Center, Projectile.velocity, Parameter);
                if (path.Bounced) EvolutionBloodSpectacleSystem.Emit(path.Bend, path.End - path.Bend, EvolutionBloodCue.Volley, boss.ThemeColor);
            }
        }
    }
}
