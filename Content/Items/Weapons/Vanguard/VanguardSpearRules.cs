using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Weapons.Vanguard;

public enum VanguardSpearKind { Vigna, Grani, WildMane, Bagpipe }

// Player-wide reserve: swapping or cloning weapons cannot manufacture a second volley.
// Deliberately independent of operator skill SP; only held spears recover it over time.
public sealed class VanguardReserve
{
    public const int Capacity = 360;
    public int Ticks { get; private set; }
    public float Ratio => Ticks / (float)Capacity;
    public bool Ready => Ticks == Capacity;
    public void Tick(bool equipped) { if (equipped && Ticks < Capacity) Ticks++; }
    public void FillFromMelee() => Ticks = Capacity;
    public bool TrySpend() { if (!Ready) return false; Ticks = 0; return true; }
    public void Clear() => Ticks = 0;
}

public static class VanguardSpearRules
{
    public const int ComboTimeout = 90;
    public const int ThrowWindup = 9;
    public const int ThrowSpacing = 8;
    public const int FlightUpdates = 3;
    public static bool ValidKind(int kind) => kind >= 0 && kind <= 3;
    public static int VolleyCount(VanguardSpearKind kind) => kind switch { VanguardSpearKind.WildMane => 4, VanguardSpearKind.Bagpipe => 5, _ => 3 };
    public static int VolleyDuration(VanguardSpearKind kind) => ThrowWindup + (VolleyCount(kind) - 1) * ThrowSpacing + 12;
    public static int ShotAt(int tick, VanguardSpearKind kind)
    {
        int elapsed = tick - ThrowWindup;
        return elapsed >= 0 && elapsed % ThrowSpacing == 0 && elapsed / ThrowSpacing < VolleyCount(kind)
            ? elapsed / ThrowSpacing : -1;
    }
    public static int Penetration(VanguardSpearKind kind) => kind == VanguardSpearKind.Grani ? 3 : kind == VanguardSpearKind.Vigna ? 1 : 2;
    public static float FlightSpeed(VanguardSpearKind kind) => 25 + (int)kind * 3;
    public static float Length(VanguardSpearKind kind) => 104 + (int)kind * 10;
    public static float StageDamage(int stage) => stage == 2 ? 1.45f : stage == 1 ? 1.12f : 1f;
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
    public static int SwingDuration(VanguardSpearKind kind, int skill, int stage, float speed)
    {
        float interval = stage == 2 ? 32 : 28;
        if (kind == VanguardSpearKind.Vigna && skill == 2) interval *= 1.5f;
        if (kind == VanguardSpearKind.WildMane && skill == 1) interval /= 1.8f;
        if (kind == VanguardSpearKind.Bagpipe && skill == 1) interval /= 1.45f;
        if (kind == VanguardSpearKind.Bagpipe && skill == 3) interval *= 1.7f;
        return (int)Math.Clamp(MathF.Ceiling(interval / Math.Clamp(speed, .5f, 3f)), 12, 110);
    }
    public static bool Active(float progress) => progress >= .2f && progress < .8f;
    // Nadir's rise -> fall -> forward thrust; no forced player displacement.
    // Rendering and collision consume the same pose, including inverted gravity.
    public static void Pose(Vector2 center, Vector2 aim, int facing, float gravity, int stage,
        float progress, float length, out Vector2 grip, out Vector2 tip)
    {
        float p = Math.Clamp(progress, 0, 1);
        float release = Math.Clamp((p - .2f) / .6f, 0, 1);
        float ease = 1 - MathF.Pow(1 - release, 2.5f);
        float angle = stage switch {
            0 => MathHelper.Lerp(1.35f, -1.35f, ease),
            1 => MathHelper.Lerp(-1.45f, 1.35f, ease),
            _ => MathHelper.Lerp(-.13f, .08f, ease)
        };
        Vector2 blade = aim.RotatedBy(angle * facing * gravity);
        float extension = stage == 2 ? 12 + 58 * MathF.Sin(release * MathHelper.Pi) : 13;
        float pull = p < .2f ? -8 * p / .2f : p > .8f ? -8 * (p - .8f) / .2f : 0;
        grip = center + aim * (extension + pull);
        tip = grip + blade * Math.Clamp(length, 60, 220) * .78f;
    }
}
