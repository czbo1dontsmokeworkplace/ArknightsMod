using System;

namespace ArknightsMod.Content.Items.Weapons.Charged;

internal static class DreadnoughtRules
{
    internal const int BaseSwingTicks = 28;
    internal const float WeaponScale = .5f;
    internal const float MaxMomentum = .5f;
    internal const float MomentumPerSwing = .025f;
    internal const int IdleGraceTicks = 60;
    internal const float MomentumDecay = MaxMomentum / 240f;

    // Smooth full turn with a sharper acceleration into the front half. The speed
    // stays positive at the rear, connecting each turn directly to the next.
    internal static float Turn(float progress)
    {
        float t = Math.Clamp(progress, 0, 1);
        return t - .6f * MathF.Sin(2f * MathF.PI * t) / MathF.PI
            + .075f * MathF.Sin(4f * MathF.PI * t) / MathF.PI;
    }

    internal static float TurnSpeed(float progress)
    {
        float t = Math.Clamp(progress, 0, 1);
        return 1f - 1.2f * MathF.Cos(2f * MathF.PI * t) + .3f * MathF.Cos(4f * MathF.PI * t);
    }

    internal static float TrailIntensity(float progress) => Math.Clamp((TurnSpeed(progress) - .1f) / 2.4f, 0, 1);

    internal static bool OpeningHit(int previousTargets) => previousTargets == 0;
    internal static float Gain(float momentum) => Math.Min(MaxMomentum, momentum + MomentumPerSwing);
    internal static float Decay(float momentum, bool switchedAway = false) =>
        Math.Max(0, momentum - MomentumDecay * (switchedAway ? 3f : 1f));
    internal static float SwingTicks(float attackSpeed, bool swiftSkill) =>
        Math.Max(8, (swiftSkill ? 21f : BaseSwingTicks) / Math.Max(.1f, attackSpeed));
}
