using System;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Phantom;

// 本模组适配数值：近战高速连切，技能等级沿用十级表。
internal static class PhantomBalance
{
    internal const int Damage = 118;
    internal const int Duration = 600;
    internal const int Redeploy = 720;
    internal const int EchoCooldown = 1500;
    internal const float EchoDamage = .65f;
    internal const float DeployRange = 220f;
    internal const float EchoLeash = 520f;
    internal const float PulseRadius = 128f;
    internal static int Rank(int rank) => Math.Clamp(rank, 0, 9);
    internal static int Layers(int rank) => rank >= 9 ? 10 : rank >= 6 ? 9 : rank >= 3 ? 8 : 7;
    private static readonly float[] LayerBonuses = [.10f, .11f, .12f, .13f, .14f, .15f, .16f, .17f, .18f, .20f];
    private static readonly float[] ShieldFractions = [.20f, .25f, .30f, .35f, .40f, .45f, .50f, .60f, .70f, .80f];
    private static readonly float[] PulseMultipliers = [1.8f, 1.9f, 2f, 2.1f, 2.2f, 2.3f, 2.4f, 2.6f, 2.8f, 3f];
    private static readonly int[] ControlTimes = [120, 120, 120, 150, 150, 150, 180, 210, 240, 270];
    internal static float LayerBonus(int rank) => LayerBonuses[Rank(rank)];
    internal static float ShieldFraction(int rank) => ShieldFractions[Rank(rank)];
    internal static float Dodge(int rank) => rank >= 9 ? .50f : rank >= 6 ? .40f : rank >= 3 ? .30f : .20f;
    internal static float PulseMultiplier(int rank) => PulseMultipliers[Rank(rank)];
    internal static int ControlTime(int rank) => ControlTimes[Rank(rank)];
}
