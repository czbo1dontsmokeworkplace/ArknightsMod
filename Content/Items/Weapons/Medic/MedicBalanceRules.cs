using System;

namespace ArknightsMod.Content.Items.Weapons.Medic;

internal static class MedicBalanceRules
{
    // Fixed by the item's recipe stage, never by the current world's progress.
    // Multi-target: rounded 350/530 (Nightingale/Shining E2 max, no trust/modules).
    // Therapist: the original branch's 80% distant-healing coefficient.
    internal const float MultiTargetMultiplier=.66f, TherapistMultiplier=.8f;
    internal static int HealingFor(MedicBranch branch,int progression)
    {
        int baseline=progression<2?20:progression<5?40:60;
        float multiplier=branch==MedicBranch.MultiTarget?MultiTargetMultiplier
            :branch==MedicBranch.Therapist?TherapistMultiplier:1f;
        return (int)MathF.Round(baseline*multiplier,MidpointRounding.AwayFromZero);
    }
}
