using System;
using Microsoft.Xna.Framework;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Items.Weapons.Guard.Crusher;

public enum CrusherKind { Quartz, WindChimes, Hoederer, Ulpianus }

internal static class CrusherRules
{
    internal const int IdleTicks = 0, FadeTicks = 8;
    // Two shared 28-tick swings, then one equally long rest: 84 ticks per pair.
    internal static int Interval(CrusherKind kind) => BladeSwingTemplate.BaseTicks * 3;
    internal static float Reach(CrusherKind kind) => Vector2.Distance(Grip(kind), Head(kind));
    internal static Color Tint(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => new(186, 157, 100), CrusherKind.WindChimes => new(73, 194, 172),
        CrusherKind.Hoederer => new(193, 40, 62), _ => new(57, 123, 201) };
    internal static Color Edge(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => new(255, 238, 191), CrusherKind.WindChimes => new(209, 255, 226),
        CrusherKind.Hoederer => new(255, 166, 100), _ => new(167, 238, 255) };
    internal static string ItemName(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => nameof(QuartzGreatsword), CrusherKind.WindChimes => nameof(WindChimesHeavyBlade),
        CrusherKind.Hoederer => nameof(HoedererGreatsword), _ => nameof(UlpianusAnchor) };
    internal static string Art(CrusherKind kind) => "ArknightsMod/Content/Items/Weapons/Guard/Crusher/" + ItemName(kind);
    // Original sprites: three blades point up/right; the anchor's crown points DOWN from its ring.
    internal static Vector2 Grip(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => new(13, 64), CrusherKind.WindChimes => new(10, 56),
        CrusherKind.Hoederer => new(10, 62), _ => new(27, 12) };
    internal static Vector2 Head(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => new(66, 1), CrusherKind.WindChimes => new(68, 1),
        CrusherKind.Hoederer => new(72, 1), _ => new(27, 78) };
    internal static float Turn(float progress)
    {
        return BladeSwingTemplate.Angle(0, 1, progress);
    }
    internal static bool Cutting(float progress) => progress >= 0 && progress < 1;
    internal static float Level(int level, float first, float last) => MathHelper.Lerp(first, last, Math.Clamp(level - 1, 0, 9) / 9f);
}
