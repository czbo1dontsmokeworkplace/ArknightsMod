using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Pure encounter rules are also exercised by the standalone validation runner.
internal static class EvolutionRules
{
    public const int TransitionTicks = 600;
    public const float ArenaRadius = 1200f;
    public const int HazardCap = 180;
    public const int PeripheralReserve = 32;
    public const int WallHalfColumns = 15;
    public const float WallSpacing = 108;
    // 节奏调整独立于伤害和本体移动倍率，单次冲刺速度/持续时间不变。
    public const float StraightShotSpeedMultiplier = .85f;
    public const float HomingTurnMultiplier = .9f;
    public static int LaserWarning(int ticks) => (int)Math.Ceiling(ticks * 1.15);
    public static int Recovery(int ticks) => (int)Math.Ceiling(ticks * 1.10);
    public static int ChargeStride(int stride, int chargeEnd) => chargeEnd + (int)Math.Ceiling((stride - chargeEnd) * 1.15);
    public static Vector2 ShotVelocity(EvolutionShot kind, Vector2 velocity) =>
        kind is EvolutionShot.Lance or EvolutionShot.Fragment ? velocity * StraightShotSpeedMultiplier : velocity;
    public static int TransitionDuration(int phase) => phase == 4 ? 720 : TransitionTicks;
    public static int CinematicTime(int phase, int timer) => phase == 4 ? timer * TransitionTicks / 720 : timer;
    public static float Aggression(int phase) => phase >= 5 ? 2.2f : phase >= 3 ? 1.6f : 1.3f;
    public static Vector2 TransitionVelocity(Vector2 position, Vector2 velocity, Vector2 target, Vector2 targetVelocity)
    {
        Vector2 offset = position - target;
        float distance = offset.Length();
        Vector2 outward = offset.SafeNormalize(new Vector2(-1, -.3f).SafeNormalize(-Vector2.UnitX));
        Vector2 desired = targetVelocity + outward * MathHelper.Clamp((600 - distance) * .05f, -32, 24);
        float maximum = Math.Min(48, Math.Max(18, targetVelocity.Length() + 12));
        if (desired.Length() > maximum) desired = desired.SafeNormalize(Vector2.Zero) * maximum;
        return Vector2.Lerp(velocity, desired, .085f);
    }
    public static int TransitionSupportWave(int timer) => timer >= 90 && timer <= 618 && (timer - 90) % 66 == 0 ? (timer - 90) / 66 : -1;
    private static readonly int[] NewbornCycle = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly EvolutionBroodGroup[][] BroodPrograms = {
        new[] { new EvolutionBroodGroup(EvolutionBrood.Spider, EvolutionBroodPreset.Hunter, 6), new EvolutionBroodGroup(EvolutionBrood.Puppet, EvolutionBroodPreset.Hunter, 2) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Spider, EvolutionBroodPreset.Rain, 6) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.GiantSpider, EvolutionBroodPreset.Siege, 2), new EvolutionBroodGroup(EvolutionBrood.Tumor, EvolutionBroodPreset.Siege, 3) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Puppet, EvolutionBroodPreset.Minefield, 3), new EvolutionBroodGroup(EvolutionBrood.Bomb, EvolutionBroodPreset.Minefield, 4) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Puppet, EvolutionBroodPreset.Weaver, 3) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Abomination, EvolutionBroodPreset.Siege, 1), new EvolutionBroodGroup(EvolutionBrood.Spider, EvolutionBroodPreset.Ambush, 4) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Tumor, EvolutionBroodPreset.Seeder, 5), new EvolutionBroodGroup(EvolutionBrood.GiantSpider, EvolutionBroodPreset.Artillery, 2) },
        new[] { new EvolutionBroodGroup(EvolutionBrood.Abomination, EvolutionBroodPreset.Conductor, 1), new EvolutionBroodGroup(EvolutionBrood.Puppet, EvolutionBroodPreset.Weaver, 2) }
    };
    public static ReadOnlySpan<EvolutionBroodGroup> NewbornGroups(int attack) => BroodPrograms[Math.Abs(attack % BroodPrograms.Length)];
    public static int NewbornDuration(int attack) => attack switch { 1 => 390, 2 or 6 => 480, 3 or 5 => 450, _ => 420 };
    // Emit dangerous central lanes first; distant scenery-like shots may use only the remaining budget.
    public static int Column(int index) => index == 0 ? 0 : (index + 1) / 2 * (index % 2 == 0 ? -1 : 1);
    private static readonly int[] EvolvedCycle = { 1, 0, 2, 5, 3, 0, 4, 5 };
    private static readonly int[] PerfectCycle = { 0, 1, 2, 5, 3, 0, 4, 5 };
    private static readonly int[] DesperateCycle = { 0, 1, 5 };
    public static int Attack(int phase, int cycle, bool desperate)
    {
        int[] attacks = desperate ? DesperateCycle : phase == 1 ? NewbornCycle : phase == 3 ? EvolvedCycle : PerfectCycle;
        return attacks[Math.Abs(cycle % attacks.Length)];
    }
    public static int MinionCap(int phase) => phase == 5 ? 4 : phase <= 2 ? 12 : 8;
    public static int Threshold(int maximum, int phase) => phase == 1 ? (int)Math.Ceiling(maximum * .6) : phase == 3 ? (int)Math.Ceiling(maximum * .2) : 0;
    public static bool InGap(float angle, float gapDirection, float halfWidth = .75f)
    {
        float delta = MathF.IEEERemainder(angle - gapDirection, MathF.PI);
        return MathF.Abs(delta) < halfWidth;
    }
    public static float RingRadius(int age) => Math.Max(0, age - 30) * 4.75f + 32f;
    public static int ContactDamage(int phase, bool charge, bool desperate = false) => EvolutionDamageCockpit.BossContactDamage(phase, charge, desperate);
}

internal enum EvolutionShot { Blood, Spirit, Beam, Rock, Tentacle, Pulse, Core, Spike, Fragment, DashMarker, Lance, CrimsonBomb, Eruption }
internal enum EvolutionBrood { Spider, GiantSpider, Puppet, Abomination, Tumor, Bomb }
internal enum EvolutionBroodPreset : byte { Hunter, Rain, Siege, Minefield, Weaver, Ambush, Seeder, Artillery, Conductor }
internal readonly record struct EvolutionBroodGroup(EvolutionBrood Kind, EvolutionBroodPreset Preset, int Count);
