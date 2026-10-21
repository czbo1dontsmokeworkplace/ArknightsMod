using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Weapons.Vanguard;

public enum VanguardSpearKind { Vigna, Grani, WildMane, Bagpipe }

// One fixed cooldown across the route, independent of items, melee hits and skill SP.
public sealed class VanguardChargeCooldown
{
    public const int Duration = 300;
    public int Remaining { get; private set; }
    public float Ratio => 1 - Remaining / (float)Duration;
    public bool Ready => Remaining == 0;
    public void Tick() { if (Remaining > 0) Remaining--; }
    public bool TryStart() { if (!Ready) return false; Remaining = Duration; return true; }
}

public static class VanguardSpearRules
{
    public const int ChargeDuration = 28;
    public const int ThrustHoldFrames = 5;
    public const float ChargeTurnRate = MathHelper.Pi / 180f; // one degree per movement tick
    public const float ChargeEndSpeed = 2f;
    public const int PulseLifetime = 36;
    public static bool ValidKind(int kind) => kind >= 0 && kind <= 3;
    public static float ChargeDamage(VanguardSpearKind kind) => kind switch {
        VanguardSpearKind.Vigna => 4f, VanguardSpearKind.Grani => 4.5f,
        VanguardSpearKind.WildMane => 5f, _ => 6f
    };
    public static float ChargeSpeed(VanguardSpearKind kind) => 24 + (int)kind * 2;
    public static float ChargeRange(VanguardSpearKind kind) => ChargeSpeed(kind) * ChargeDuration;
    public static float Length(VanguardSpearKind kind) => 104 + (int)kind * 10;
    // Independent random rolls control emission count, cone angle, speed and lift.
    // Even a downward/inverted-gravity thrust retains forward momentum.
    public static Vector2 SparkLaunchVelocity(Vector2 aim, float gravity, bool heavy, float angle, float speed, float lift)
    {
        aim = aim.SafeNormalize(Vector2.UnitX);
        angle = Math.Clamp(angle, 0, 1); speed = Math.Clamp(speed, 0, 1); lift = Math.Clamp(lift, 0, 1);
        if (heavy)
            return (angle * MathHelper.TwoPi).ToRotationVector2() * (4 + speed * 4) + aim * 1.5f;
        return aim.RotatedBy((angle - .5f) * 1.05f) * (6 + speed * 5)
            - Vector2.UnitY * (gravity < 0 ? -1 : 1) * (.5f + lift * 1.7f);
    }
    public static Color Tint(VanguardSpearKind kind) => kind switch {
        VanguardSpearKind.Vigna => new(235, 60, 92), VanguardSpearKind.Grani => new(95, 193, 244),
        VanguardSpearKind.WildMane => new(246, 142, 65), _ => new(255, 200, 91)
    };
    public static float AttackMultiplier(VanguardSpearKind kind, int skill) => (kind, skill) switch {
        (VanguardSpearKind.Vigna, 1) => 1.8f, (VanguardSpearKind.Vigna, 2) => 3f,
        (VanguardSpearKind.Grani, 2) => 1.8f, (VanguardSpearKind.WildMane, 2) => 1.8f,
        (VanguardSpearKind.Bagpipe, 1) => 1.45f, (VanguardSpearKind.Bagpipe, 2) => 2f,
        (VanguardSpearKind.Bagpipe, 3) => 2.2f, _ => 1
    };
    public static int ThrustDuration(VanguardSpearKind kind, int skill, float speed)
    {
        float interval = 28;
        if (kind == VanguardSpearKind.Vigna && skill == 2) interval *= 1.5f;
        if (kind == VanguardSpearKind.WildMane && skill == 1) interval /= 1.8f;
        if (kind == VanguardSpearKind.Bagpipe && skill == 1) interval /= 1.45f;
        if (kind == VanguardSpearKind.Bagpipe && skill == 3) interval *= 1.7f;
        // Half of the former duration, including its speed limits and skill modifiers.
        int previous = (int)Math.Clamp(MathF.Ceiling(interval / Math.Clamp(speed, .5f, 3f)), 12, 110);
        return Math.Max(11, (previous + 1) / 2);
    }
    public static bool Active(float progress) => progress >= .2f && progress < .8f;
    public static float ThrustExtension(float age, int duration)
    {
        duration = Math.Max(11, duration);
        int forward = (duration - ThrustHoldFrames) / 2;
        if (age < forward) return Math.Clamp(age / forward, 0, 1);
        if (age < forward + ThrustHoldFrames) return 1;
        float retract = Math.Clamp((age - forward - ThrustHoldFrames + 1)
            / (duration - forward - ThrustHoldFrames), 0, 1);
        return 1 - retract * retract * (3 - 2 * retract);
    }
    // Linear extension, exactly five ticks at the tip, then a smooth curved return.
    public static void ThrustPose(Vector2 hand, Vector2 aim, float progress, float length,
        out Vector2 grip, out Vector2 tip, int duration = 14)
    {
        float p = Math.Clamp(progress, 0, 1);
        float extension = ThrustExtension(p * duration, duration) * 52;
        grip = hand + aim * (extension - 18);
        tip = grip + aim * Math.Clamp(length, 60, 220) * .78f;
    }
}
