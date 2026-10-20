using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal enum TacticalShot { None, Boundary, Snipe, Fan, Circle, Rail, Mine, Pursuit }

// Two joint 180-tick operations. The surrounding time carries individual transition fire;
// escorts keep moving and never need the entire team to arrive before they can shoot.
internal static class MaterialistTactics
{
    internal const int RoundTicks = 180;
    internal const float BoundaryLength = 720;
    internal static bool Active(int timer) => timer >= 60 && timer < 420;
    internal static int Beat(int timer) => timer < 60 ? -1 : (timer - 60) % RoundTicks;
    internal static int Phase(int cap) => cap <= 6 ? 1 : cap <= 8 ? 2 : 3;
    internal static int DiveBeat(int slot) => 108 + slot % 2 * 10;
    internal static bool Bomber(WingDuty duty, int slot) => duty == WingDuty.Interceptor || duty == WingDuty.Scatter && slot % 2 == 1;
    internal static int BombDiveBeat(int slot) => slot == 5 ? 100 : 82;
    internal static bool Diving(WingPattern pattern, int timer, int slot, int cap)
    {
        WingDuty duty = MaterialistRules.Duty(Phase(cap), slot);
        if (pattern == WingPattern.BombRun && Active(timer) && Bomber(duty, slot))
            return Beat(timer) >= BombDiveBeat(slot) && Beat(timer) < BombDiveBeat(slot) + 26;
        if (!Active(timer) || MaterialistRules.Duty(Phase(cap), slot) != WingDuty.Interceptor ||
            pattern is not (WingPattern.RelayDive or WingPattern.HeavyHunt or WingPattern.Scissor or WingPattern.FinalVolley)) return false;
        int beat = Beat(timer), start = DiveBeat(slot);
        return beat >= start && beat < start + 20;
    }
    internal static bool Spinning(WingPattern pattern, int timer, int slot, int cap) => Active(timer) &&
        MaterialistRules.Duty(Phase(cap), slot) == WingDuty.Scatter &&
        (pattern is WingPattern.SpinVolley or WingPattern.FinalVolley or WingPattern.Spiral) && Beat(timer) is >= 66 and < 108;
    internal static TacticalShot Shot(WingDuty duty, WingPattern pattern, int beat, int slot)
    {
        if (beat < 0 || beat >= 150) return TacticalShot.None;
        if (pattern == WingPattern.BombRun && Bomber(duty, slot)) return TacticalShot.None;
        if (duty == WingDuty.Containment) return beat == 28 ? (pattern == WingPattern.RailCorridor ? TacticalShot.Rail : TacticalShot.Boundary) : TacticalShot.None;
        if (duty == WingDuty.Precision) return beat == 52 + slot % 2 * 12 ? TacticalShot.Snipe : TacticalShot.None;
        if (duty == WingDuty.Interceptor) return TacticalShot.None;
        if (duty == WingDuty.Heavy) return beat == 72 ? TacticalShot.Pursuit : beat == 126 ? TacticalShot.Fan : TacticalShot.None;
        if (pattern == WingPattern.MineWeave) return beat is 44 or 100 ? TacticalShot.Mine : TacticalShot.None;
        if (pattern == WingPattern.PursuitNet) return beat == 64 + slot % 2 * 16 ? TacticalShot.Pursuit : TacticalShot.None;
        if (pattern is WingPattern.SpinVolley or WingPattern.FinalVolley or WingPattern.Spiral)
            return beat is >= 66 and < 108 && (beat - 66) % 8 == 0 ? TacticalShot.Circle : TacticalShot.None;
        return beat == 72 + slot % 2 * 10 || beat == 102 + slot % 2 * 10 || beat == 132 + slot % 2 * 6 ? TacticalShot.Fan : TacticalShot.None;
    }
    internal static Vector2 Position(WingPattern pattern, int timer, int slot, int cap, Vector2 center, float exit)
    {
        WingDuty duty = MaterialistRules.Duty(Phase(cap), slot);
        Vector2 f = exit.ToRotationVector2(), n = f.RotatedBy(-MathHelper.PiOver2);
        float side = slot % 2 == 0 ? -1 : 1;
        int beat = Math.Max(0, Beat(timer));
        Vector2 local;
        if (pattern == WingPattern.BombRun && Bomber(duty, slot))
        {
            int start = BombDiveBeat(slot);
            float bombSide = slot == 3 ? -1 : 1;
            Vector2 from = new(bombSide * 440, slot == 5 ? -100 : 15), to = new(-bombSide * 440, slot == 5 ? 65 : 130);
            if (beat < start) local = from + new Vector2(0, MathF.Sin(Math.Min(beat, 45) * .07f) * 25);
            else if (beat < start + 26) local = Vector2.Lerp(from, to, (beat - start) / 26f);
            else local = MaterialistFormation.Bezier(to, to + new Vector2(0, -180), from + new Vector2(0, -180), from,
                MaterialistFormation.Smooth((beat - start - 26) / 50f));
            return center + n * local.X + f * local.Y;
        }
        switch (duty)
        {
            case WingDuty.Containment:
                float width = pattern == WingPattern.Scissor ? MathHelper.Lerp(310, 245, MaterialistFormation.Smooth((beat - 65) / 75f)) : 285;
                local = new Vector2(side * width, -260);
                if (pattern == WingPattern.TrackingLance && slot == 0) local = new Vector2(-360, -170);
                break;
            case WingDuty.Precision:
                local = new Vector2(side * 125, -225);
                if (pattern == WingPattern.Battery) local = new Vector2(side * 65, -190 - slot % 2 * 75);
                if (beat < 40) local.X += MathF.Sin(beat * .08f) * 24;
                if (pattern is WingPattern.Gate or WingPattern.ShieldRelay)
                    local += new Vector2(MathF.Sin(beat * .04f + slot) * 40, MathF.Cos(beat * .04f + slot) * 30);
                break;
            case WingDuty.Scatter:
                local = new Vector2(side * 205, -25);
                if (pattern == WingPattern.PrismCross)
                    local = new Vector2(side * MathHelper.Lerp(205, 120, MaterialistFormation.Smooth((beat - 60) / 85f)), -120 + beat);
                if (pattern == WingPattern.MineWeave)
                    local += new Vector2(side * MathF.Sin(beat * .025f) * 25, MathF.Sin(beat * .05f) * 90);
                if (pattern == WingPattern.Battery)
                    local += new Vector2(MathF.Cos(beat * .04f), MathF.Sin(beat * .04f)) * 48;
                else if (pattern is WingPattern.Scissor or WingPattern.ArcPincer)
                    local += new Vector2(side * MathF.Sin(beat * .035f) * 30, MathF.Sin(beat * .06f) * 50);
                else if (pattern == WingPattern.Spiral)
                {
                    float a = side * (1.5f + MathHelper.Clamp(beat / 150f, 0, 1) * 1.2f);
                    local = new Vector2(MathF.Sin(a), -MathF.Cos(a)) * (175 + MathHelper.Clamp(beat / 150f, 0, 1) * 75);
                }
                if (Spinning(pattern, timer, slot, cap))
                {
                    float a = (beat - 66) / 42f * MathHelper.TwoPi;
                    local += new Vector2(MathF.Cos(a) - 1, MathF.Sin(a)) * 36;
                }
                break;
            case WingDuty.Interceptor:
                int start = DiveBeat(slot);
                Vector2 from = new(side * 215, -115), to = new(-side * 95, 175);
                if (pattern == WingPattern.Battery)
                    local = new Vector2(MathF.Cos(beat * .035f + slot) * 95, -190 + MathF.Sin(beat * .035f + slot) * 65);
                else if (pattern is WingPattern.Gate or WingPattern.ShieldRelay or WingPattern.ArcPincer or WingPattern.SpinVolley or WingPattern.Spiral or WingPattern.HerdRam or WingPattern.RailCorridor or WingPattern.MineWeave or WingPattern.PrismCross or WingPattern.PursuitNet or WingPattern.TrackingLance)
                    local = new Vector2(side * (160 + MathF.Sin(beat * .035f) * 35), -100 + MathF.Sin(beat * .05f + slot) * 65);
                else if (beat < start) local = from + new Vector2(MathF.Sin(Math.Min(beat, 60) * .05f) * side * 24, 0);
                else if (beat < start + 20) local = Vector2.Lerp(from, to, (beat - start) / 20f);
                else local = MaterialistFormation.Bezier(to, to + new Vector2(side * 160, 65), from + new Vector2(side * 130, 30), from,
                    MaterialistFormation.Smooth((beat - start - 20) / 45f));
                break;
            default:
                local = new Vector2(side * 410, -235);
                break;
        }
        return center + n * local.X + f * local.Y;
    }
}
