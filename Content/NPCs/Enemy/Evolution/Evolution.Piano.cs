using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal enum EvolutionPianoFigure { Chord = 8, Glissando, Arpeggio, Trill, Staccato, Reprise }

internal readonly record struct EvolutionPianoNote(int Tick, int Lane, bool FromBottom, bool Staccato = false);

internal static class EvolutionPianoRules
{
    private static readonly int[] evenChord = { -6, -4, -2, 0, 2, 4, 6, -5, -3, -1, 1, 3, 5 };
    private static readonly int[] oddChord = { -5, -3, -1, 1, 3, 5, -6, -4, -2, 0, 2, 4, 6 };
    internal const int Duration = 234;
    internal const int PreviousDuration = 390;
    internal const int HalfKeys = 6;
    internal const float KeySpacing = 144;
    internal const int Warning = 40, Active = 18, StaccatoActive = 6, Fade = 36;
    internal static bool IsFigure(int attack) => attack >= (int)EvolutionPianoFigure.Chord && attack <= (int)EvolutionPianoFigure.Reprise;
    internal static bool IsStyle(EvolutionBeamStyle style) => style is EvolutionBeamStyle.Piano or EvolutionBeamStyle.PianoStaccato;
    internal static int GapStart(int cycle) => Math.Abs(cycle % 3) switch { 0 => -2, 1 => 0, _ => 1 };
    internal static bool InGap(int lane, int cycle) => lane == GapStart(cycle) || lane == GapStart(cycle) + 1;
    internal static int NoteCount => 12;

    private static int ChordLane(int index, int cycle, bool odd)
    {
        // 奇偶音列交替；被安全通道占用的音位移到外围，和弦仍保留六个声部。
        int count = 0;
        foreach (int lane in odd ? oddChord : evenChord)
            if (!InGap(lane, cycle) && count++ == index) return lane;
        return 6;
    }
    internal static EvolutionPianoNote Note(int attack, int cycle, int index)
    {
        if (!IsFigure(attack) || index < 0 || index >= NoteCount) throw new ArgumentOutOfRangeException(nameof(index));
        int phrase = index / 6, step = index % 6;
        bool bottom = phrase == 1;
        int chord = ChordLane(step, cycle, bottom);
        return (EvolutionPianoFigure)attack switch
        {
            EvolutionPianoFigure.Chord => new(phrase == 0 ? 14 : 82, chord, bottom),
            EvolutionPianoFigure.Glissando => new((phrase == 0 ? 8 : 72) + step * 6,
                ChordLane(bottom ? 5 - step : step, cycle, false), bottom),
            EvolutionPianoFigure.Arpeggio => new((phrase == 0 ? 8 : 64) + step * 9,
                ChordLane((step * 2 + step / 3) % 6, cycle, bottom), bottom),
            EvolutionPianoFigure.Trill => new((phrase == 0 ? 10 : 64) + step * 8,
                step % 2 == 0 ? GapStart(cycle) - 1 - phrase : GapStart(cycle) + 2 + phrase, bottom),
            EvolutionPianoFigure.Staccato => new((phrase == 0 ? 14 : 78) + step / 3 * 24, chord, bottom, true),
            _ => new(phrase == 0 ? 14 : 70 + step * 6, chord, bottom)
        };
    }
}

public sealed partial class Evolution
{
    private void DoPianoFigure()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (int index = 0; index < EvolutionPianoRules.NoteCount; index++)
        {
            EvolutionPianoNote note = EvolutionPianoRules.Note(Attack, (int)NPC.ai[2], index);
            if (Timer != note.Tick) continue;
            Vector2 source = new(PatternOrigin.X + note.Lane * EvolutionPianoRules.KeySpacing,
                note.FromBottom ? CorridorBottom - 12 : CorridorTop + 12);
            Shoot(EvolutionShot.Beam, source, note.FromBottom ? -Vector2.UnitY : Vector2.UnitY,
                EvolutionRules.CorridorHeight - 24, EvolutionPianoRules.Warning, 140, quietLaunch: true,
                beamStyle: note.Staccato ? EvolutionBeamStyle.PianoStaccato : EvolutionBeamStyle.Piano);
        }
    }

    private void DrawPianoKeyboard()
    {
        if (Phase != 3 || CrashOpeningActive || !EvolutionPianoRules.IsFigure(Attack) || Timer < 0 || PatternSerial != Serial) return;
        float fade = Math.Clamp(Timer / 12f, 0, 1) * Math.Clamp((EvolutionPianoRules.Duration - Timer) / 18f, 0, 1);
        for (int lane = -EvolutionPianoRules.HalfKeys; lane <= EvolutionPianoRules.HalfKeys; lane++)
        {
            float x = PatternOrigin.X + lane * EvolutionPianoRules.KeySpacing;
            bool gap = EvolutionPianoRules.InGap(lane, (int)NPC.ai[2]);
            Color tint = gap ? new Color(245, 184, 134) : EvolutionVisuals.Blood;
            for (int edge = 0; edge < 2; edge++)
            {
                float y = edge == 0 ? CorridorTop + 7 : CorridorBottom - 7;
                EvolutionVisuals.Glow(new Vector2(x, y), tint * (fade * (gap ? .12f : .2f)), new Vector2(92, 15));
                EvolutionVisuals.Line(new Vector2(x - 35, y), new Vector2(x + 35, y), tint * (.3f * fade), 2, false);
            }
        }
    }
}

internal static class EvolutionPianoVisuals
{
    private static ulong lastStrike = ulong.MaxValue;
    internal static void Strike(Vector2 center)
    {
        if (Main.dedServ || lastStrike == Main.GameUpdateCount) return;
        lastStrike = Main.GameUpdateCount;
        SoundEngine.PlaySound(SoundID.Item33 with { Volume = .36f, Pitch = -.25f, MaxInstances = 2 }, center);
    }
}
