using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Guard.ArtsFighter;

// Normal-attack timing reference: AugerHoldout, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
// No Calamity types or assets are loaded.
internal static class ArtsFighterSwing
{
    internal const int BaseUseTime = 16;
    internal static float Ease(float p, float accelerate, float brake)
    {
        p = MathHelper.Clamp(p, 0f, 1f);
        return p < .5f ? .5f * MathF.Pow(p * 2f, accelerate)
            : 1f - .5f * MathF.Pow((1f - p) * 2f, brake);
    }
    private const int ComboLength = 3;
    // Three alternating cuts, short recovery between cuts, then a three-unit recovery.
    internal static bool Step(ref float timer, ref float count, float unit,
        ref float angle, ref float bladePower)
    {
        unit = Math.Max(2f, unit);
        float cooldown = -unit * (count >= ComboLength ? 3f : 1f);
        if (timer < 0f)
        {
            if (count >= ComboLength && timer == -1f) count = 0f;
            timer++;
            bool longRecovery = count >= ComboLength;
            float progress = Utils.GetLerpValue(cooldown, -1f, timer, true);
            if (longRecovery && timer <= cooldown * .35f)
                bladePower = MathHelper.Lerp(bladePower, 0f, progress);
            else
                bladePower = MathHelper.Lerp(bladePower, 1.5f,
                    Utils.GetLerpValue(longRecovery ? cooldown * .35f : cooldown, -1f, timer, true));
            if (timer == 0f) count++;
            else
            {
                float direction = count % 2f != 0f ? 1f : -1f;
                angle = MathHelper.Lerp(-MathHelper.Pi * direction, -.75f * MathHelper.Pi * direction,
                    Ease(progress, 2f, 2f));
            }
            return false;
        }
        if (timer >= unit)
        {
            timer = cooldown + 1f;
            return false;
        }
        float swingDirection = count % 2f != 0f ? -1f : 1f;
        float swingProgress = Utils.GetLerpValue(0f, unit - 1f, timer, true);
        angle = MathHelper.Lerp(-.75f * MathHelper.Pi * swingDirection, MathHelper.Pi * swingDirection,
            Ease(swingProgress, 5f, 3f));
        bool release = timer == MathF.Floor((unit - 1f) * .5f) + 1f;
        timer++;
        return release;
    }
}
