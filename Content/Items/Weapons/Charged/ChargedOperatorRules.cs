using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.Charged;

public enum ChargedOperatorKind { Melantha, Matoimaru, Franka, NearlRadiant, Ambriel, Andreana, Firewatch, Fartooth }

// Pure timing/geometry rules shared by input, collision, visuals and offline validation.
public static class ChargedOperatorRules
{
    public const int DreadnoughtChargeTicks = 60;
    public const int SniperLeftInterval = 60;
    public static bool SniperLeftReady(bool leftHeld, bool rightHeld, ulong now, ulong nextShot)
        => leftHeld && !rightHeld && now >= nextShot;
    public static bool IsMelee(ChargedOperatorKind kind) => kind <= ChargedOperatorKind.NearlRadiant;
    public static int ChargeTicks(ChargedOperatorKind kind) => IsMelee(kind) ? 60 : 90;
    public static float Completion(float ticks, int maximum) => Math.Clamp(ticks / Math.Max(1, maximum), 0f, 1f);
    public static float ShotMultiplier(float charge) => charge >= 1f ? 3.6f : .5f + .5f * Math.Clamp(charge / .8f, 0f, 1f);
    public static float ShotWidth(float charge) => MathHelper.Lerp(6f, 22f, Math.Clamp(charge, 0, 1));
    public static float MovementMultiplier(float charge) => MathHelper.Lerp(1f, .12f, Math.Clamp(charge, 0, 1));
    public static float Reach(ChargedOperatorKind kind, bool heavy) => heavy ? 150f + (int)kind * 30f : 76f + (int)kind * 8f;
    public static float ShotRange(ChargedOperatorKind kind, int skill) =>
        kind == ChargedOperatorKind.Fartooth && skill == 3 || kind == ChargedOperatorKind.Ambriel && skill == 2
        ? 3200f : 1600f + ((int)kind - 4) * 200f;
    public static Color ColorFor(ChargedOperatorKind kind) => kind switch {
        ChargedOperatorKind.Melantha => new(196, 76, 139),
        ChargedOperatorKind.Matoimaru => new(147, 229, 203),
        ChargedOperatorKind.Franka => new(255, 127, 49),
        ChargedOperatorKind.NearlRadiant => new(255, 214, 98),
        ChargedOperatorKind.Ambriel => new(241, 155, 194),
        ChargedOperatorKind.Andreana => new(136, 113, 205),
        ChargedOperatorKind.Firewatch => new(124, 203, 155),
        _ => new(236, 221, 149)
    };
}
