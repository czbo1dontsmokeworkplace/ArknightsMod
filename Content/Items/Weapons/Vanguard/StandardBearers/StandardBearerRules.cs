using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;

public enum StandardBearerKind { Myrtle, Elysium, Jacint, Saileach }

public static class StandardBearerRules
{
    public const int AttackFrames = 54;
    public const float AuraRange = 800;
    public const float Reach = 96;
    public const int WaveSize = 32;
    public const int WaveLife = 12;
    public const int WaveTravelFrames = 9;
    public const float WaveSpeed = 8;
    // Including a 32x32 hitbox's farthest corner: 72 + sqrt(16^2+16^2) < 96 px.
    public static float WaveDistance(float age) => Math.Clamp(age, 0, WaveTravelFrames) * WaveSpeed;
    public static int CostTotal(StandardBearerKind kind, int skill) => (kind, skill) switch {
        (StandardBearerKind.Myrtle, 0) => 14,
        (StandardBearerKind.Myrtle, 1) => 16,
        (StandardBearerKind.Elysium, 1) => 20,
        (StandardBearerKind.Jacint, 1) => 27,
        (StandardBearerKind.Saileach, 1) => 20,
        (StandardBearerKind.Saileach, 2) => 10,
        _ => 18
    };
    public static int Earned(int total, int elapsed, int duration) =>
        (int)((long)Math.Clamp(elapsed, 0, Math.Max(1, duration)) * total / Math.Max(1, duration));
    private static float Ease(float t) { t = Math.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
    // Upright -> eased turn -> short thrust -> retract -> eased upright return.
    public static void Pose(float progress, float aim, float gravity, out float angle, out float extension)
    {
        float rest = -MathHelper.PiOver2 * gravity;
        float turn = progress < .3f ? Ease(progress / .3f)
            : progress < .68f ? 1 : 1 - Ease((progress - .68f) / .32f);
        angle = rest + MathHelper.WrapAngle(aim - rest) * turn;
        extension = progress < .3f || progress > .68f ? 0
            : progress < .46f ? Ease((progress - .3f) / .16f) * 30
            : 30 * (1 - Ease((progress - .46f) / .22f));
    }
    public static bool CanStrike(float progress) => progress >= .3f && progress < .68f;
    public static Color Tint(StandardBearerKind kind) => kind switch {
        StandardBearerKind.Myrtle => new(145, 235, 135),
        StandardBearerKind.Elysium => new(125, 205, 255),
        StandardBearerKind.Jacint => new(240, 160, 195),
        _ => new(255, 218, 120)
    };
}
