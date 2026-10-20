using System;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal enum FlightOrder { Arrival, Deploy, Command, Bombard, Sweep, Ram, Recover, Transition, Overload, Death }
internal enum DroneShot { Bolt, Missile, Bombardment, Beam, Prism, Seeker, FluxMine, RailSlug, TrailBomb, ChaseBeam, ArcSpark, Petal, Lattice, CoreLance, ClusterOrb, OrbitSpark, Needle, MachineRound, GravityOrb, ScanBurst, Beacon, DropMissile, CommandRocket, CannonShell, ArtilleryBlast, ScoutPulse, ArtsOrb }
internal enum WingRole { Drone, MkII, ArtsA1, ArtsA2, Raptor }
internal enum WingDuty { Containment, Precision, Scatter, Interceptor, Heavy }
internal enum FireStyle { Standard, Containment, Precision, Suppression, CoreSpread, CoreFocus }
internal enum WingPattern { ArcPincer, SpinVolley, RelayDive, Scissor, Battery, Gate, ShieldRelay, HeavyHunt, Spiral, HerdRam, FinalVolley, RailCorridor, MineWeave, PrismCross, PursuitNet, BombRun, TrackingLance }
internal enum CorePattern { TwinSpiral, BurstRings, SquareLattice, Rose, AimedFan, NeedleVolley, ScissorArcs, SineRain, OrbCascade, Pinwheel, Diamond, ConvergingLances }
internal enum ArtilleryPattern { None, RocketRelay, WalkingBarrage, Crossfire }

// All times are game ticks. Projectile values are engine base damage (before difficulty/defence).
internal static class MaterialistRules
{
    internal const int Life = 72000;
    internal const int Defence = 38;
    internal const int HazardCap = 192;
    internal const int TransitionTicks = 150;
    internal const int ArrivalTicks = 210;
    internal const int DeathTicks = 120;
    internal const int EscortLifetime = 5400;
    internal const int EscortGrace = 72;
    internal const int LaserWarning = 60;
    internal const int BombWarning = 78;
    internal const float BombRadius = 96;
    internal const float BeamLength = 1400;
    internal const float BeamWidth = 16;
    internal const float Mobility = .8f;
    internal const int ScanWarning = 59; // Previously 84: 30% shorter, rounded to a whole tick.
    internal const int ScoutLifetime = 144;
    internal const int ArtsOrbLifetime = 156;
    internal const float ArtsOrbSpeed = 7.6f;
    internal const float SpreadSpacing = 1.2f;
    internal static int SpreadRingCount(int count) => Math.Max(1, (int)Math.Floor(count / SpreadSpacing));
    internal const int MachineUpdates = 16;

    private static readonly FlightOrder[] Phase1 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Recover };
    private static readonly FlightOrder[] Phase2 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Recover };
    private static readonly FlightOrder[] Phase3 = { FlightOrder.Deploy, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Command, FlightOrder.Recover };

    internal static WingPattern Pattern(int phase, int cycle) => phase switch {
        1 => Math.Abs(cycle % 7) switch { 2 => WingPattern.SpinVolley, 3 => WingPattern.RelayDive, 4 => WingPattern.BombRun, 5 => WingPattern.TrackingLance, _ => WingPattern.ArcPincer },
        2 => Math.Abs(cycle % 8) switch { 2 => WingPattern.Battery, 3 => WingPattern.Gate, 4 => WingPattern.RailCorridor, 5 => WingPattern.MineWeave, 6 => WingPattern.ShieldRelay, _ => WingPattern.Scissor },
        _ => Math.Abs(cycle % 8) switch { 2 => WingPattern.Spiral, 3 => WingPattern.HerdRam, 4 => WingPattern.PrismCross, 5 => WingPattern.PursuitNet, 6 => WingPattern.FinalVolley, _ => WingPattern.HeavyHunt }
    };
    internal static int PatternDuration(WingPattern pattern) => 480;
    internal static WingDuty Duty(int phase, int slot) => slot < 2 ? WingDuty.Containment : phase == 1 ?
        (slot == 2 ? WingDuty.Precision : slot < 5 ? WingDuty.Scatter : WingDuty.Interceptor) :
        slot < 4 ? WingDuty.Precision : slot < 6 ? WingDuty.Scatter : slot < 8 ? WingDuty.Interceptor : WingDuty.Heavy;

    internal static FlightOrder Order(int phase, int cycle)
    {
        var sequence = phase == 1 ? Phase1 : phase == 2 ? Phase2 : Phase3;
        return sequence[Math.Abs(cycle % sequence.Length)];
    }
    internal static int Threshold(int lifeMax, int phase) => phase == 1 ? (int)(lifeMax * .65f) : phase == 2 ? (int)(lifeMax * .30f) : 0;
    internal static int WingCap(int phase) => phase == 1 ? 6 : phase == 2 ? 8 : 10;
    internal static int Duration(FlightOrder order) => order switch {
        FlightOrder.Arrival => ArrivalTicks, FlightOrder.Deploy => 300, FlightOrder.Command => 480,
        FlightOrder.Bombard => 270, FlightOrder.Sweep => 240, FlightOrder.Ram => 270,
        FlightOrder.Overload => 270, FlightOrder.Recover => 180,
        FlightOrder.Transition => TransitionTicks, FlightOrder.Death => DeathTicks, _ => 180
    };
    internal static int Damage(DroneShot shot) => shot switch {
        DroneShot.Bolt => 30, DroneShot.Missile => 34, DroneShot.Bombardment => 42, DroneShot.Beam => 38,
        DroneShot.Prism => 32, DroneShot.Seeker => 34, DroneShot.FluxMine => 32, DroneShot.RailSlug => 30,
        DroneShot.TrailBomb => 36, DroneShot.ChaseBeam => 38, DroneShot.ArcSpark => 32, DroneShot.Petal => 32,
        DroneShot.Lattice => 32, DroneShot.CoreLance => 38, DroneShot.ClusterOrb => 36, DroneShot.OrbitSpark => 32, DroneShot.Needle => 34,
        DroneShot.MachineRound => 28, DroneShot.GravityOrb => 34, DroneShot.ScanBurst => 40, DroneShot.DropMissile => 38,
        DroneShot.CommandRocket => 36, DroneShot.CannonShell => 38, DroneShot.ArtilleryBlast => 40,
        DroneShot.ScoutPulse => 28, DroneShot.ArtsOrb => 34, _ => 0
    };
    internal static int WingLife(WingRole role) => role switch {
        WingRole.Drone => 1400, WingRole.MkII => 1800, WingRole.ArtsA1 => 3300,
        WingRole.ArtsA2 => 4200, WingRole.Raptor => 9600, _ => 1400
    };
}
