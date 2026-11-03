using System;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Items.Weapons.Charged;

internal static class DreadnoughtRules
{
    internal const int BaseSwingTicks = BladeSwingTemplate.BaseTicks;
    internal const float WeaponScale = .5f;
    internal const int ReadyTicks = 180;

    // Smooth full turn with a sharper acceleration into the front half. The speed
    // stays positive at the rear, connecting each turn directly to the next.
    internal static float Turn(float progress)
    {
        return BladeSwingTemplate.Turn(progress);
    }

    internal static float TurnSpeed(float progress)
    {
        float t = Math.Clamp(progress, 0, 1);
        return 1f - 1.2f * MathF.Cos(2f * MathF.PI * t) + .3f * MathF.Cos(4f * MathF.PI * t);
    }

    internal static float TrailIntensity(float progress) => Math.Clamp((TurnSpeed(progress) - .1f) / 2.4f, 0, 1);

    internal static bool CanStrike(int previousTargets) => previousTargets == 0;
    internal static float SwingTicks(float attackSpeed, bool swiftSkill) =>
        BladeSwingTemplate.Duration(attackSpeed, swiftSkill);
}
