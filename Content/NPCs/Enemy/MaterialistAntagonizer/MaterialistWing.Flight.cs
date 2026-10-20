using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private int flightRoute = -1, blendAge;
    private Vector2 blendStart, lastFlightGoal, goalVelocity;
    private bool goalReady, maneuverLocked;
    private Vector2 maneuverCenter;
    private float maneuverExit;
    private Vector2 FlightCenter(MaterialistAntagonizer boss) => maneuverLocked ? maneuverCenter : boss.FormationCenter;
    private float FlightExit(MaterialistAntagonizer boss) => maneuverLocked ? maneuverExit : boss.ExitAngle;

    private void BeginFlightBlend(NPC npc, MaterialistAntagonizer boss)
    {
        blendStart = npc.Center - boss.FormationCenter;
        blendAge = 0;
    }
    private void PrepareFlight(NPC npc, MaterialistAntagonizer boss)
    {
        int round = Math.Max(0, (boss.Timer - 60) / 180);
        int route = boss.Revision * 8 + round;
        if (flightRoute != route)
        {
            flightRoute = route;
            maneuverLocked = false;
            BeginFlightBlend(npc, boss);
        }
        int beat = MaterialistTactics.Beat(boss.Timer), start = -1, end = -1;
        if (boss.Order == FlightOrder.Command && MaterialistTactics.Active(boss.Timer) && !boss.ArtilleryActive)
        {
            if (boss.ScoutReady && boss.ScoutAttack(Slot) == ScoutMove.BombDash)
            {
                beat = MaterialistScoutPlanner.LocalBeat(boss, Slot);
                start = 41; end = 128;
            }
            else if (!boss.ScoutReady && MaterialistRules.Duty(boss.Phase, Slot) == WingDuty.Interceptor &&
                boss.Pattern is WingPattern.RelayDive or WingPattern.HeavyHunt or WingPattern.Scissor or WingPattern.FinalVolley)
            {
                start = MaterialistTactics.DiveBeat(Slot) - 45;
                end = MaterialistTactics.DiveBeat(Slot) + 32;
            }
        }
        if (start >= 0 && beat >= start && beat < end && !maneuverLocked && Age >= MaterialistRules.EscortGrace)
        {
            maneuverLocked = true;
            maneuverCenter = boss.FormationCenter;
            maneuverExit = boss.ExitAngle;
            if (MaterialistAntagonizer.Authority) npc.netUpdate = true;
        }
        else if (maneuverLocked && (start < 0 || beat >= end))
        {
            maneuverLocked = false;
            BeginFlightBlend(npc, boss);
            if (MaterialistAntagonizer.Authority) npc.netUpdate = true;
        }
    }
    private Vector2 BlendFlightGoal(NPC npc, MaterialistAntagonizer boss, Vector2 goal)
    {
        if (maneuverLocked) return goal;
        int length = 28 + Slot % 4 * 7;
        float amount = MaterialistFormation.Smooth(++blendAge / (float)length);
        return boss.FormationCenter + Vector2.Lerp(blendStart, goal - boss.FormationCenter, amount);
    }
    private void MoveEscort(NPC npc, MaterialistAntagonizer boss, Vector2 goal, float baseSpeed)
    {
        Vector2 rawVelocity = goalReady ? goal - lastFlightGoal : boss.NPC.velocity;
        lastFlightGoal = goal;
        goalReady = true;
        float targetSpeed = boss.TrackingVelocity.Length();
        rawVelocity = MaterialistFlight.Limit(rawVelocity, Math.Min(158, targetSpeed + baseSpeed + 10));
        goalVelocity = Vector2.Lerp(goalVelocity, rawVelocity, .30f);
        float acceleration = .8f + Math.Min(targetSpeed, 96) * .055f + (baseSpeed >= 30 ? 1.6f : 0);
        if (Role(npc) == WingRole.Raptor) acceleration *= .55f;
        float maximumSpeed = MathHelper.Clamp(targetSpeed * 1.2f + 44, 48, 162);
        npc.velocity = MaterialistFlight.Follow(npc.Center, npc.velocity, goal, goalVelocity,
            baseSpeed, targetSpeed, acceleration, maximumSpeed, .115f);
    }
    private void FireTransitCover(NPC npc, MaterialistAntagonizer boss, bool executing, Vector2 goal)
    {
        WingRole role = Role(npc);
        if (role is WingRole.ArtsA1 or WingRole.ArtsA2 or WingRole.Raptor) return;
        if (!MaterialistAntagonizer.Authority || !boss.EscortFire || Age < MaterialistRules.EscortGrace || maneuverLocked) return;
        int beat = MaterialistTactics.Beat(boss.Timer);
        float lag = Vector2.Distance(npc.Center, goal);
        bool changing = !executing || beat < 35 || beat >= 142 || lag > 240;
        if (!changing) return;
        Player player = Main.player[boss.NPC.target];
        float distance = npc.Distance(player.Center);
        if (distance < 110 || distance > 2200) return;
        if (executing && lag < 400 && beat < 142) return;
        int cadence = 60 + Slot % 3 * 6;
        if ((Main.GameUpdateCount + (ulong)(Slot * 9)) % (ulong)cadence != 0) return;
        Vector2 muzzle = Muzzle(npc);
        Vector2 point = player.Center + Vector2.Clamp(player.velocity * 8, new Vector2(-150), new Vector2(150));
        Vector2 axis = (point - muzzle).SafeNormalize(Vector2.UnitY);
        int count = role == WingRole.MkII ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = axis.RotatedBy((i - (count - 1) * .5f) * .15f * MaterialistRules.SpreadSpacing) * 8.5f;
            boss.Shoot(DroneShot.Bolt, muzzle, velocity, duration: 100, shooter: npc.whoAmI, style: FireStyle.Suppression);
        }
    }
}
