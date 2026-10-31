using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons.BlastCasters;

public enum BlastCasterKind { Corroserum, Ifrit, Aphrissa }

// Tick-based rates. Optical velocity is PER SUBUPDATE, never pixels per game tick.
internal static class BlastCasterRules
{
    internal const int OpticalUpdates = 16;
    internal const float OpticalStep = 12f;
    internal const int OpticalHistory = 24;
    // PhasedGodRay's 20px contact/body scale, enlarged by the requested 1.5.
    internal const float FluxScale = 1.5f;
    internal const float FluxContactRadius = 10f * FluxScale;
    internal const float FluxAfterglow = 7f;
    internal const float RelayPlacementRange = 560f;
    internal const float RelayTetherRange = 720f;
    internal const int RelayLife = 1200;
    internal static int Interval(BlastCasterKind kind) => kind switch
    { BlastCasterKind.Corroserum => 4, BlastCasterKind.Ifrit => 3, _ => 2 };
    internal static float Range(int mode) => mode == 1 ? 1680f : 1248f;
    internal static float FlightRange(BlastCasterKind kind, int mode)
        => kind == BlastCasterKind.Aphrissa ? Range(mode) : kind == BlastCasterKind.Ifrit ? 752f : 547f;
    internal static int Updates(BlastCasterKind kind) => OpticalUpdates;
    internal static float Step(BlastCasterKind kind) => OpticalStep;
    internal static float FlightTicks(BlastCasterKind kind,int mode,bool redirected=false)
        => (FlightRange(kind,mode)+(redirected?320:0))/(OpticalStep*OpticalUpdates);
    internal static float ShotStep(BlastCasterKind kind, int mode)
        => OpticalStep;
    internal static float ShotSize(BlastCasterKind kind, int mode)
        => SizeMultiplier(kind) * (kind == BlastCasterKind.Ifrit && mode == 2 ? 1.5f
            : kind == BlastCasterKind.Aphrissa && mode == 2 ? 1.4f : 1f);
    internal static float SizeMultiplier(BlastCasterKind kind) => kind switch
    { BlastCasterKind.Ifrit => 1.2f, BlastCasterKind.Aphrissa => 1.5f, _ => 1f };
    internal static Color FluxAccent(BlastCasterKind kind) => kind switch
    { BlastCasterKind.Corroserum => new(238, 205, 72), BlastCasterKind.Ifrit => new(255, 159, 54), _ => new(240, 201, 92) };
    internal static bool ConsumesGel(int volley) => volley % 3 == 0;
    internal static float RankScale(int rank) => (Math.Clamp(rank, 1, 10) - 1) / 9f;
    internal static float DamageMultiplier(BlastCasterKind kind, int mode, int rank)
    {
        float r = RankScale(rank);
        return (kind, mode) switch
        {
            (BlastCasterKind.Corroserum, 1) => 1.35f + .45f * r,
            (BlastCasterKind.Corroserum, 2) => 1.15f + .35f * r,
            (BlastCasterKind.Ifrit, 1) => 1.15f + .25f * r,
            (BlastCasterKind.Ifrit, 2) => 2.2f + .8f * r,
            (BlastCasterKind.Ifrit, 3) => 1.5f + .6f * r,
            (BlastCasterKind.Aphrissa, 1) => 1.3f + .4f * r,
            (BlastCasterKind.Aphrissa, 2) => 2f + r,
            (BlastCasterKind.Aphrissa, 3) => 1.5f + .5f * r,
            _ => 1f
        };
    }
    internal static float RateMultiplier(BlastCasterKind kind, int mode, int rank)
        => mode == 1 && kind == BlastCasterKind.Ifrit ? 1.3f + .2f * RankScale(rank)
        : mode == 3 && kind == BlastCasterKind.Aphrissa ? 1.5f : 1f;
    internal static bool ChargedShot(BlastCasterKind kind, int index)
        => index == 1 && kind != BlastCasterKind.Corroserum;
    internal static Color Tint(BlastCasterKind kind) => kind switch
    { BlastCasterKind.Corroserum => new(153, 229, 77), BlastCasterKind.Ifrit => new(255, 117, 31), _ => new(66, 225, 212) };
    internal static Color Spectrum(float phase)
    {
        float p = (phase % 1f + 1f) % 1f * 3;
        Color a = new(54, 220, 218), b = new(166, 232, 66), c = new(255, 156, 49);
        return p < 1 ? Color.Lerp(a, b, p) : p < 2 ? Color.Lerp(b, c, p - 1) : Color.Lerp(c, a, p - 2);
    }
    // Shared by held sprite and actual muzzle. Placeholder art is a horizontal gun.
    internal const string GunTexture = "ArknightsMod/Content/Items/Weapons/Sniper/Fiammetta/FiammettaHandCannon";
    internal static float GunWidth(BlastCasterKind kind) => 58f + (int)kind * 10f;
    internal static Vector2 Grip => new(.29f, .58f);
    internal static Vector2 Barrel => new(.98f, .45f);
    internal static Vector2 MuzzleOffset(Vector2 textureSize, BlastCasterKind kind, float angle, int facing)
    {
        Vector2 local = (Barrel - Grip) * textureSize * (GunWidth(kind) / textureSize.X);
        local.Y *= facing;
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(local.X * c - local.Y * s, local.X * s + local.Y * c);
    }
}
