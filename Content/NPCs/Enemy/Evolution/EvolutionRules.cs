using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Pure encounter rules are also exercised by the standalone validation runner.
internal static class EvolutionRules
{
    public const int TransitionTicks = 600;
    public const float ArenaRadius = 1200f;
    public const float CorridorHeight = 50 * 16;
    public const float EvolvedProjectileMultiplier = .85f;
    public const int HazardCap = 180;
    public const int PeripheralReserve = 32;
    public const int WallHalfColumns = 15;
    public const float WallSpacing = 144;
    // 节奏调整独立于伤害；冲刺通过延长行程扩大范围，最高速度不变。
    public const float StraightShotSpeedMultiplier = .85f * .9f;
    public const float HomingTurnMultiplier = .4f;
    // 基础激光预警56帧，伤害时钟另加淡出中点8帧；三阶段再整体压缩至80%。
    public static int LaserWarning(int ticks) => Math.Max(56, (int)Math.Ceiling(ticks * 1.15));
    public const int LaserFadeTicks = 16;
    public const int AxisActiveTicks = 16 * 60;
    public const int AxisRayCount = 3;
    public const float AxisPeakSpeed = .00126f * 4 * 1.3f;
    public const float LaserReachMultiplier = 1.3f;
    public const float AxisReach = 1750 * LaserReachMultiplier;
    public const float AxisDamageReach = AxisReach * .9f;
    public const int SpinBombSetup = 72, SpinBombDuration = 210;
    public const float EnrageMultiplier = 1.3f;
    public static int ReactionTime(int ticks, bool perfect, float boost = 1) =>
        Math.Max(1, (int)MathF.Round(ticks * (perfect ? .8f : 1) / boost));
    public static int ProjectileWarning(EvolutionShot kind, int ticks, bool perfect, float boost = 1)
    {
        int original = IsLaser(kind) ? LaserWarning(ticks) + LaserFadeTicks / 2 : ticks;
        int result = ReactionTime(original, perfect, boost);
        return IsLaser(kind) ? Math.Max(1, result - LaserFadeTicks / 2) : result;
    }
    public static float BeamEnvelope(int age, int active, int fade, bool instant = false)
    {
        if (age < 0) return 0;
        float grow = instant ? 1 : MathHelper.SmoothStep(0, 1, Math.Clamp((age + 1) / 7f, 0, 1));
        float tail = 1 - MathHelper.SmoothStep(0, 1, Math.Clamp((age - active) / (float)fade, 0, 1));
        return grow * tail;
    }
    public static float SpinBombAngle(int active) => Math.Clamp(active / (float)(SpinBombDuration - 1), 0, 1) * MathHelper.TwoPi * 2;
    public static bool SpinBombEmission(int active,bool enraged)
    {
        if(active<0||active>=SpinBombDuration)return false;
        int count=enraged?39:30;
        return (active+1)*count/SpinBombDuration>active*count/SpinBombDuration;
    }
    public static int DashEmissionCount(int age, int duration, int variant, bool perfect, bool enraged)
    {
        if (!perfect) return variant == 2 ? (age >= 8 && age % 9 == 8 ? 1 : 0) :
            (age == duration / 3 || age == duration * 2 / 3 ? 1 : 0);
        int events = variant == 2 ? 6 : 3; // 原4次赤雾→6次；原2处裂环/残刃→3处，均为150%。
        if (enraged) events = (int)MathF.Round(events * EnrageMultiplier);
        int count = 0;
        for (int i = 0; i < events; i++) if (age == (i + 1) * duration / (events + 1)) count++;
        return count;
    }
    public const int AxisChargeTicks = 90;
    public const int AxisSetupTicks = 60;
    public const float AxisVisualWidth = 450;
    public const float AxisCollisionWidth = 120;
    public static int AxisLockEnd => AxisSetupTicks + FireTime(EvolutionShot.Beam, ProjectileWarning(EvolutionShot.Beam,AxisChargeTicks,true)) + AxisActiveTicks;
    // 光轴侧射每84帧一阵：前35帧六对依次由内向外发射，之后留49帧空档。
    public const int AxisBladeBurstTicks = 84;
    public static int AxisBladePair(int activeAge)
    {
        int elapsed = activeAge - 30;
        if (elapsed < 0 || 30 + elapsed / AxisBladeBurstTicks * AxisBladeBurstTicks + 35 >= AxisActiveTicks) return -1;
        for (int pair = 0; pair < 6; pair++)
            if (elapsed % AxisBladeBurstTicks == pair * 7) return pair;
        return -1;
    }
    public static int AxisBladeLane(int activeAge)
    {
        int pair = AxisBladePair(activeAge);
        return pair >= 0 ? 2 + pair / 2 : int.MinValue;
    }
    public static int AxisBladeRay(int activeAge)
    {
        int pair = AxisBladePair(activeAge);
        return pair >= 0 ? (pair + (activeAge - 30) / AxisBladeBurstTicks) % AxisRayCount : -1;
    }
    public const int ChargeLockTicks = 54;
    public static int ChargeActiveTicks(bool perfect) => perfect ? 44 : 52;
    public static float ChargeDistance(bool perfect, bool desperate) => ChargeActiveTicks(perfect) * (perfect ? 36 * 2.2f : 32 * 1.6f) * (desperate ? 1.1f : 1);
    public static bool IsLaser(EvolutionShot kind) => kind is EvolutionShot.Beam or EvolutionShot.Spike or EvolutionShot.Eruption;
    public static int FireTime(EvolutionShot kind, int delay) => delay + (kind == EvolutionShot.Radiation ? 16 : IsLaser(kind) ? LaserFadeTicks / 2 : 4);
    // 预警淡出中点（50%亮度）与真实光束开火使用同一时钟。
    public static float LaserWarningOpacity(int age, int delay)
    {
        float fadeIn = MathHelper.SmoothStep(0, 1, MathHelper.Clamp(age / (float)Math.Min(20, Math.Max(1, delay)), 0, 1));
        float fadeOut = 1 - MathHelper.SmoothStep(0, 1, MathHelper.Clamp((age - delay) / (float)LaserFadeTicks, 0, 1));
        return fadeIn * fadeOut;
    }
    // 对平滑速度曲线积分，靠同步时钟求绝对角度，换向时既不跳角也不跳速。
    private static float AxisRampArea(float u) => u * u * u - .5f * u * u * u * u;
    private static float AxisSweep(int age)
    {
        float t = Math.Clamp(age, 0, 480);
        if (t <= 120) return AxisPeakSpeed * 120 * AxisRampArea(t / 120);
        if (t <= 360) return AxisPeakSpeed * (60 + t - 120);
        float u = (t - 360) / 120;
        return AxisPeakSpeed * (300 + 120 * (u - AxisRampArea(u)));
    }
    public static float AxisAngle(int activeAge) => -MathHelper.PiOver2 +
        (activeAge <= 480 ? AxisSweep(activeAge) : AxisSweep(480) - AxisSweep(activeAge - 480));
    public static float PerfectChargePower(float progress) => MathHelper.Lerp(.72f, 1, MathHelper.SmoothStep(0, 1, MathHelper.Clamp(progress / .45f, 0, 1)));
    public static int Recovery(int ticks) => (int)Math.Ceiling(ticks * 1.10 * 1.10);
    public static int ChargeStride(int stride, int chargeEnd) => chargeEnd + (int)Math.Ceiling((stride - chargeEnd) * 1.15 * 1.10);
    public static int ChargeVariant(int cycle, int attackIndex) => (cycle + attackIndex + 2) % 3;
    public static int VolleyInterval(int ticks) => (int)Math.Ceiling(ticks * 1.10);
    // 偶数、奇数扇面都保留中央空槽，但不删除任何弹幕。
    public static int FanSlot(int index, int count) => index < count / 2 ? index - count / 2 : index - count / 2 + 1;
    public static Vector2 FormationDrift(Vector2 velocity, int fallbackSide = 1) =>
        new((Math.Abs(velocity.X) > .5f ? Math.Sign(velocity.X) : fallbackSide) * MathHelper.Clamp(Math.Abs(velocity.X) * .3f, 1.6f, 3), 0);
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
    private static readonly int[] EvolvedCycle = { 8, 14, 19, 15, 9, 14, 16, 15, 10, 14, 20, 15,
        11, 14, 22, 15, 12, 14, 17, 15, 13, 14, 21, 15, 18, 14, 4, 15, 9, 14, 5, 15 };
    private static readonly int[] PerfectCycle = { 6, 0, 1, 7, 2, 5, 3, 0, 4, 5 };
    private static readonly int[] DesperateCycle = { 0, 6, 1, 7, 5 };
    public static int Attack(int phase, int cycle, bool desperate)
    {
        int[] attacks = desperate ? DesperateCycle : phase == 1 ? NewbornCycle : phase == 3 ? EvolvedCycle : PerfectCycle;
        return attacks[Math.Abs(cycle % attacks.Length)];
    }
    public static int MinionCap(int phase) => phase >= 4 ? 4 : phase == 3 ? 10 : 12;
    public static int Threshold(int maximum, int phase) => phase == 1 ? (int)Math.Ceiling(maximum * .8) : phase == 3 ? (int)Math.Ceiling(maximum * .4) : 0;
    public static bool InGap(float angle, float gapDirection, float halfWidth = .75f)
    {
        float delta = MathF.IEEERemainder(angle - gapDirection, MathF.PI);
        return MathF.Abs(delta) < halfWidth;
    }
    public static float RingRadius(int age) => Math.Max(0, age - 30) * 4.75f + 32f;
    public static int ContactDamage(int phase, bool charge, bool desperate = false) => EvolutionDamageCockpit.BossContactDamage(phase, charge, desperate);
}

internal enum EvolutionShot { Blood, Spirit, Beam, Rock, Tentacle, Pulse, Core, Spike, Fragment, DashMarker, Lance, CrimsonBomb, Eruption, Radiation, Reflection, CrashFan, Flame, BroodRocket, GravityFlare, Serpent, BroodSpit, SpinBomb, Barrage, Ricochet }
internal enum EvolutionBeamStyle : byte { Standard, Pulse, Axis, Piano, PianoStaccato, Refracted }
internal enum EvolutionBrood { Spider, GiantSpider, Puppet, Abomination, Tumor, Bomb }
internal enum EvolutionBroodPreset : byte { Hunter, Rain, Siege, Minefield, Weaver, Ambush, Seeder, Artillery, Conductor }
internal readonly record struct EvolutionBroodGroup(EvolutionBrood Kind, EvolutionBroodPreset Preset, int Count);
