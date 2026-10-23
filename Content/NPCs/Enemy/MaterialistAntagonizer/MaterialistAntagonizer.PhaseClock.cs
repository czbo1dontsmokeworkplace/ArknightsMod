using System;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal int PhaseAge;
    internal const int PhaseLimit = 180 * 60;

    private bool UpdatePhaseClock()
    {
        if (!Authority || Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death) return false;
        if (++PhaseAge < PhaseLimit) return false;
        // Recall is not a kill: withdrawing gunships do not drop their destruction bombs.
        ClearHazards(); RecallWing();
        FinalStand = false; Shield = ShieldMax = CoreExposure = 0;
        if (Phase < 3)
        {
            NPC.life = Math.Min(NPC.life, LockedPhaseLife);
            ChangePhase();
        }
        else
        {
            NPC.life = 1;
            Enter(FlightOrder.Death);
        }
        return true;
    }
}
