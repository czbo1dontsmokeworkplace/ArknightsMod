using System;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal enum FlightOrder { Arrival, Deploy, Command, Bombard, Sweep, Ram, Recover, Transition, Overload, Death }
internal enum DroneShot { Bolt, Missile, Bombardment, Beam }
internal enum WingRole { Drone, MkII, ArtsA1, ArtsA2, Raptor }

// All times are game ticks. Projectile values are engine base damage (before difficulty/defence).
internal static class MaterialistRules
{
    internal const int Life = 72000;
    internal const int Defence = 38;
    internal const int HazardCap = 90;
    internal const int TransitionTicks = 150;
    internal const int ArrivalTicks = 150;
    internal const int DeathTicks = 120;
    internal const int EscortLifetime = 2100;
    internal const int EscortGrace = 72;
    internal const int LaserWarning = 60;
    internal const int BombWarning = 78;
    internal const float BombRadius = 96;
    internal const float BeamLength = 1400;
    internal const float BeamWidth = 16;

    private static readonly FlightOrder[] Phase1 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Bombard, FlightOrder.Command, FlightOrder.Recover };
    private static readonly FlightOrder[] Phase2 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Bombard, FlightOrder.Command, FlightOrder.Sweep, FlightOrder.Recover };
    private static readonly FlightOrder[] Phase3 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Bombard, FlightOrder.Command, FlightOrder.Ram, FlightOrder.Overload, FlightOrder.Recover };

    internal static FlightOrder Order(int phase, int cycle)
    {
        var sequence = phase == 1 ? Phase1 : phase == 2 ? Phase2 : Phase3;
        return sequence[Math.Abs(cycle % sequence.Length)];
    }
    internal static int Threshold(int lifeMax, int phase) => phase == 1 ? (int)(lifeMax * .65f) : phase == 2 ? (int)(lifeMax * .30f) : 0;
    internal static int WingCap(int phase) => phase == 1 ? 3 : phase == 2 ? 4 : 5;
    internal static int Duration(FlightOrder order) => order switch {
        FlightOrder.Arrival => ArrivalTicks, FlightOrder.Deploy => 210, FlightOrder.Command => 480,
        FlightOrder.Bombard => 270, FlightOrder.Sweep => 240, FlightOrder.Ram => 180,
        FlightOrder.Overload => 270, FlightOrder.Recover => 180,
        FlightOrder.Transition => TransitionTicks, FlightOrder.Death => DeathTicks, _ => 180
    };
    internal static int Damage(DroneShot shot) => shot switch {
        DroneShot.Bolt => 30, DroneShot.Missile => 34, DroneShot.Bombardment => 42, DroneShot.Beam => 38, _ => 0
    };
    internal static int WingLife(WingRole role) => role switch {
        WingRole.Drone => 1400, WingRole.MkII => 1800, WingRole.ArtsA1 => 2200,
        WingRole.ArtsA2 => 2800, WingRole.Raptor => 6000, _ => 1400
    };
}
