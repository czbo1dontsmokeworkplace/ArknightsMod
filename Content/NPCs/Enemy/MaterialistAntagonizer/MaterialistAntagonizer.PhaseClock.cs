namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal int PhaseAge;
    private void UpdatePhaseClock()
    {
        // Retain elapsed time for synchronized presentation; only life/escort gates advance a phase.
        if (Authority && Order is not (FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death) && PhaseAge < int.MaxValue)
            PhaseAge++;
    }
}
