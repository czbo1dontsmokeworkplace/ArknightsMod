using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal enum ScoutMove : byte
{
    None, LaneBeam, PulseGate, ChaseLance, LockBurst, SplitOrb, OrbitVolley,
    Fan, SpinRing, Crescent, BombDash, Strafe, CurveDive
}

internal static class MaterialistScoutPlanner
{
    private static readonly WingPattern[] templates = { WingPattern.ArcPincer, WingPattern.SpinVolley, WingPattern.RelayDive, WingPattern.BombRun, WingPattern.TrackingLance };
    private static readonly ScoutMove[] boundary = { ScoutMove.Strafe, ScoutMove.Crescent, ScoutMove.Fan };
    private static readonly ScoutMove[] precision = { ScoutMove.LockBurst, ScoutMove.SplitOrb, ScoutMove.OrbitVolley };
    private static readonly ScoutMove[] scatter = { ScoutMove.Fan, ScoutMove.SpinRing, ScoutMove.Crescent, ScoutMove.Strafe };
    private static readonly ScoutMove[] mobile = { ScoutMove.CurveDive, ScoutMove.Strafe, ScoutMove.SpinRing, ScoutMove.OrbitVolley };

    internal static void Roll(MaterialistAntagonizer boss)
    {
        if (!MaterialistAntagonizer.Authority || boss.Phase != 1) return;
        int choice = Main.rand.Next(templates.Length);
        if (boss.ScoutRevision > 0 && templates[choice] == boss.ScoutFormation)
            choice = (choice + Main.rand.Next(1, templates.Length)) % templates.Length;
        WingPattern template = templates[choice];
        int living = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.TryGetGlobalNPC<MaterialistWing>(out var wing) && wing.BelongsTo(boss) && !wing.Retreating && wing.Slot < 6)
                living |= 1 << wing.Slot;
        // Exactly one living scout can own a bomber special; all range-locking lasers are reserved for A2.
        bool laser = false; // Range-locking lasers now belong exclusively to Arts Master A2.
        int[] candidates = new int[6];
        int count = 0, previousSpecial = -1;
        for (int slot = 0; slot < 6; slot++)
        {
            if (boss.ScoutAttack(slot) is ScoutMove.ChaseLance or ScoutMove.BombDash) previousSpecial = slot;
            if ((living & (1 << slot)) != 0 && (laser ? slot < 2 : slot >= 3)) candidates[count++] = slot;
        }
        int special = count == 0 ? -1 : candidates[Main.rand.Next(count)];
        if (count > 1 && special == previousSpecial)
            for (int i = 0; i < count; i++) if (candidates[i] != previousSpecial) { special = candidates[i]; break; }
        uint moves = 0, offsets = 0;
        int rings = 0;
        for (int slot = 0; slot < 6; slot++)
        {
            if ((living & (1 << slot)) == 0) continue;
            ScoutMove move;
            if (slot == special) move = laser ? ScoutMove.ChaseLance : ScoutMove.BombDash;
            else
            {
                ScoutMove[] pool = slot < 2 ? boundary : slot == 2 ? precision : slot < 5 ? scatter : mobile;
                int index = Main.rand.Next(pool.Length);
                if (pool[index] == boss.ScoutAttack(slot)) index = (index + Main.rand.Next(1, pool.Length)) % pool.Length;
                move = pool[index];
                // The template biases a member's pick; it does not replace individual choices.
                if (template == WingPattern.SpinVolley && slot is 3 or 4 && Main.rand.NextBool()) move = ScoutMove.SpinRing;
                if (template == WingPattern.RelayDive && slot == 5) move = ScoutMove.CurveDive;
            }
            if (move == ScoutMove.SpinRing && rings++ >= 2) move = ScoutMove.Strafe;
            moves |= (uint)move << (slot * 4);
            offsets |= (uint)Main.rand.Next(4) << (slot * 2);
        }
        boss.ScoutFormation = template;
        boss.ScoutMoves = moves;
        boss.ScoutOffsets = offsets;
        boss.ScoutRevision++;
        boss.NPC.netUpdate = true;
    }
    internal static int LocalBeat(MaterialistAntagonizer boss, int slot) => MaterialistTactics.Beat(boss.Timer) - boss.ScoutOffset(slot);
    internal static bool Diving(ScoutMove move, int beat) => move == ScoutMove.BombDash && beat is >= 86 and < 112;
    internal static bool Spinning(ScoutMove move, int beat) => move == ScoutMove.SpinRing && beat is >= 58 and < 100;
    internal static Vector2 Position(ScoutMove move, int beat, int slot, Vector2 center, float exit)
    {
        Vector2 f = exit.ToRotationVector2(), n = f.RotatedBy(-MathHelper.PiOver2);
        float side = slot % 2 == 0 ? -1 : 1;
        Vector2 local = slot < 2 ? new Vector2(side * 285, -250) : slot == 2 ? new Vector2(-90, -230) : new Vector2(side * 210, -30);
        switch (move)
        {
            case ScoutMove.PulseGate:
                local = new Vector2(side * (290 + MathF.Sin(beat * .035f) * 24), -170 + MathF.Sin(beat * .045f) * 35);
                break;
            case ScoutMove.ChaseLance:
                local = new Vector2(side * 350, -175);
                break;
            case ScoutMove.OrbitVolley:
                local += new Vector2(MathF.Cos(beat * .045f), MathF.Sin(beat * .045f)) * 50;
                break;
            case ScoutMove.SplitOrb:
                local += new Vector2(MathF.Sin(beat * .03f) * 70, MathF.Sin(beat * .06f) * 25);
                break;
            case ScoutMove.SpinRing:
                float spin = MathHelper.Clamp((beat - 58) / 42f, 0, 1) * MathHelper.TwoPi;
                local += new Vector2(MathF.Cos(spin) - 1, MathF.Sin(spin)) * 34;
                break;
            case ScoutMove.Crescent:
                local += new Vector2(side * MathF.Sin(beat * .03f) * 55, MathF.Sin(beat * .06f) * 65);
                break;
            case ScoutMove.Strafe:
                float t = MaterialistFormation.Smooth((beat - 35) / 100f);
                local = new Vector2(side * (300 - 160 * t), -170 + t * 285 + MathF.Sin(t * MathHelper.Pi) * 35);
                break;
            case ScoutMove.BombDash:
                Vector2 from = new(side * 390, -75), to = new(-side * 390, 150);
                if (beat < 86) local = from;
                else if (beat < 112) local = Vector2.Lerp(from, to, (beat - 86) / 26f);
                else local = MaterialistFormation.Bezier(to, to + new Vector2(0, -160), from + new Vector2(0, -160), from,
                    MaterialistFormation.Smooth((beat - 112) / 42f));
                break;
            case ScoutMove.CurveDive:
                float curve = MaterialistFormation.Smooth((beat - 42) / 100f);
                local = MaterialistFormation.Bezier(new Vector2(side * 280, -170), new Vector2(side * 80, 220),
                    new Vector2(-side * 230, 130), new Vector2(side * 240, -160), curve);
                break;
        }
        return center + n * local.X + f * local.Y;
    }
}
