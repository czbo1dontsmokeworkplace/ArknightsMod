using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal readonly record struct EvolutionRecitalNote(int Tick, float X, float Height, Vector2 Velocity,
    EvolutionShot Kind, float Parameter, int Warning, int Life, EvolutionBeamStyle Style = EvolutionBeamStyle.Standard);

internal static class EvolutionRecitalRules
{
    internal const int Duration = 420;
    internal static bool IsFigure(int attack) => attack is >= 27 and <= 32;
    internal static int Count(int attack) => attack switch { 27 => 36, 28 => 60, 29 => 56, 30 => 72, 31 => 12, 32 => 48, _ => 0 };
    private static int SkipPair(int step, int first) => step < first ? step : step + 2;
    internal static EvolutionRecitalNote Note(int attack, int cycle, int index)
    {
        if (index < 0 || index >= Count(attack)) throw new ArgumentOutOfRangeException(nameof(index));
        // Every phrase uses a snapshot of the player's X. Warnings never chase the player.
        int variation = Math.Abs(cycle % 3);
        if (attack == 27) // Staircase chords: three voices answer at a time, with two empty rows.
        {
            int measure = index / 9, step = index % 9, side = measure % 2 == 0 ? -1 : 1;
            int row = SkipPair(step, 3 + (measure + variation) % 3);
            return new(12 + measure * 58 + step / 3 * 8, side * 1000, row / 10f,
                -side * Vector2.UnitX, EvolutionShot.Beam, 2400, 40, 124, EvolutionBeamStyle.PianoStaccato);
        }
        if (attack == 28) // Five-note arpeggios travel across the corridor in alternating phrases.
        {
            int group = index / 5, step = index % 5, side = group % 2 == 0 ? -1 : 1;
            float height = .5f + MathF.Sin((group + variation) * .72f) * .34f;
            Vector2 velocity = (-side * Vector2.UnitX).RotatedBy((step - 2) * .09f) * (9 + group % 3);
            return new(12 + group * 14, side * 900, height, velocity, EvolutionShot.Barrage, 1400, 42, 172);
        }
        if (attack == 29) // Two voices: top and bottom enter eighteen ticks apart, sharing a gap.
        {
            int wave = index / 14, voice = index % 14 / 7, step = index % 7;
            int lane = SkipPair(step, 2 + (wave + variation) % 3) - 4;
            return new(12 + wave * 52 + step * 5 + voice * 18, lane * 180, voice,
                (voice == 0 ? Vector2.UnitY : -Vector2.UnitY) * 12,
                EvolutionShot.Lance, EvolutionRules.CorridorHeight - 160, 44, 144);
        }
        if (attack == 30) // Petal rings have six absent slots; successive rings answer from the other wing.
        {
            int ring = index / 18, step = index % 18, side = ring % 2 == 0 ? -1 : 1;
            float opening = side < 0 ? 0 : MathHelper.Pi;
            float angle = opening + (step + 3.5f) * MathHelper.TwoPi / 24;
            return new(18 + ring * 52 + step / 6 * 5, side * 600, ring % 2 == 0 ? .35f : .65f,
                angle.ToRotationVector2() * (7 + ring * .5f), EvolutionShot.ScoreSeed, 26 + ring * 4, 36, 176);
        }
        if (attack == 31) // Refraction canon: three diagonal rays, exactly one bounce apiece.
        {
            int phrase = index / 3, step = index % 3, side = phrase % 2 == 0 ? -1 : 1;
            bool upper = phrase % 2 == 0;
            return new(16 + phrase * 54 + step * 10, side * (600 + step * 160), upper ? .08f : .92f,
                new Vector2(-side * (.36f + step * .13f), upper ? 1 : -1).SafeNormalize(Vector2.UnitY),
                EvolutionShot.Beam, 2400, 40, 176, EvolutionBeamStyle.Refracted);
        }
        // Accelerating tremolo: three curved seeds and one short vertical accent per beat.
        int pulse = index / 4, note = index % 4, wing = pulse % 2 == 0 ? -1 : 1;
        int tick = 12 + pulse * 18 - pulse * (pulse - 1) / 4;
        if (note == 3)
            return new(tick + 8, (pulse % 2 == 0 ? -1 : 1) * (360 + pulse % 3 * 180), 0,
                Vector2.UnitY, EvolutionShot.Beam, EvolutionRules.CorridorHeight - 160, 40, 124, EvolutionBeamStyle.PianoStaccato);
        return new(tick, wing * 850, .25f + pulse % 3 * .25f,
            (-wing * Vector2.UnitX).RotatedBy((note - 1) * .19f) * (9 + pulse % 3),
            EvolutionShot.ScoreSeed, 22, 36, 166);
    }

    internal static Vector2 Source(EvolutionRecitalNote note, Vector2 snapshot, float top, float bottom) =>
        new(snapshot.X + note.X, MathHelper.Lerp(top + 80, bottom - 80, note.Height));

    internal static Vector2 SeedPoint(Vector2 origin, Vector2 velocity, float amplitude, float phase, float time)
    {
        float t = Math.Max(0, time);
        Vector2 normal = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        return origin + velocity * t + normal * (amplitude * MathF.Sin(t * .06f + phase) * Math.Clamp(t / 14, 0, 1));
    }
}

public sealed partial class Evolution
{
    private void DoRecital()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (int index = 0; index < EvolutionRecitalRules.Count(Attack); index++)
        {
            EvolutionRecitalNote note = EvolutionRecitalRules.Note(Attack, (int)NPC.ai[2], index);
            if (Timer != note.Tick) continue;
            Shoot(note.Kind, EvolutionRecitalRules.Source(note, PatternOrigin, CorridorTop, CorridorBottom),
                note.Velocity, note.Parameter, note.Warning, note.Life, beamStyle: note.Style, quietLaunch: true);
        }
    }
}
