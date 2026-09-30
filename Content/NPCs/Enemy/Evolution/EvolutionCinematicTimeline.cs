using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Local presentation state only. Never stores or changes encounter clocks, damage or entity counts.
internal struct EvolutionCinematicPlayback
{
    internal int PreviousPhase, LastTimer, PlayedCues, ExitStyle, ExitClock;
    internal Vector2 ExitOrigin;

    internal bool Update(int phase, int timer, int clock, Vector2 center)
    {
        bool exiting = (PreviousPhase == 2 && phase == 3) || (PreviousPhase == 4 && phase == 5);
        if (exiting) { ExitStyle = PreviousPhase; ExitClock = clock; ExitOrigin = center; }
        if (phase != PreviousPhase)
        {
            // Joining an already-running phase reconstructs the field, but does not replay old sounds.
            LastTimer = timer - 1; PlayedCues = 0;
            if (!exiting) ExitStyle = 0;
        }
        PreviousPhase = phase;
        return exiting;
    }

    internal bool Cue(int timer, int cue, int bit)
    {
        int mask = 1 << bit;
        bool play = (PlayedCues & mask) == 0 && LastTimer < cue && timer >= cue && timer - cue <= 6;
        if (timer >= cue) PlayedCues |= mask;
        return play;
    }

    internal bool TryFrame(int phase, int timer, int clock, Vector2 center, out EvolutionCinematicFrame frame)
    {
        if (phase is 2 or 4)
        {
            // A client awaiting a phase packet must not continue growing the effect indefinitely.
            frame = new(phase, Math.Clamp(timer, 0, 599), center); return true;
        }
        int age = 600 + Math.Max(0, clock - ExitClock);
        if (ExitStyle != 0 && age < EvolutionCinematicFrame.EndTime && phase == ExitStyle + 1)
        { frame = new(ExitStyle, age, ExitOrigin); return true; }
        frame = default; return false;
    }
}

internal readonly struct EvolutionCinematicFrame
{
    internal const int BurstTime = 568;
    internal const int EndTime = 672;
    internal const int MaximumMotes = 192;
    internal readonly int Style;
    internal readonly float Time;
    internal readonly Vector2 Center;
    internal EvolutionCinematicFrame(int style, float time, Vector2 center) { Style = style; Time = time; Center = center; }
    internal bool Perfect => Style == 4;
    internal float Entrance => Smooth(0, 42, Time);
    internal float Fade => 1 - Smooth(604, EndTime, Time);
    internal float Charge => Smooth(360, BurstTime, Time) * (1 - Smooth(BurstTime, BurstTime + 12, Time));
    internal float Reveal => Smooth(BurstTime, 600, Time);
    internal float Burst => Time >= BurstTime ? MathF.Pow(1 - Smooth(BurstTime, BurstTime + 46, Time), 2) : 0;
    internal float Scale => Perfect ? 1.18f : 1f;
    internal int MoteCount(bool reduced) => reduced ? 48 : Perfect ? MaximumMotes : 144;
    internal float Atmosphere => Entrance * Fade * (.08f + .13f * Charge);
    internal float Shake => Time < BurstTime ? Charge * 1.5f : Burst * (Perfect ? 5.5f : 4f);
    internal float ExposureDark => Smooth(515, 553, Time) * (1 - Smooth(561, 568, Time)) * (Perfect ? .30f : .23f);
    internal float ExposureFlash => Time >= 568 && Time < 582 ?
        (1 - Smooth(570, 582, Time)) * Smooth(568, 570, Time) * (Perfect ? .34f : .27f) : 0;
    internal static float Smooth(float start, float end, float value)
    {
        float t = MathHelper.Clamp((value - start) / (end - start), 0, 1);
        return t * t * (3 - 2 * t);
    }
    internal static float Hash(int index)
    {
        uint n = unchecked((uint)index * 747796405u + 2891336453u);
        n = ((n >> (int)((n >> 28) + 4)) ^ n) * 277803737u;
        return ((n >> 22) ^ n) / (float)uint.MaxValue;
    }
    internal Vector2 Inflow(int index, float time)
    {
        float period = 125 + Hash(index + 93) * 80;
        float u = time / period + Hash(index + 19);
        u -= MathF.Floor(u);
        int arms = Perfect ? 6 : 4;
        float angle = index % arms * MathHelper.TwoPi / arms + (1 - u) * (Perfect ? -4.2f : 3.2f) + time * .004f;
        angle += (Hash(index + 31) - .5f) * .25f;
        float radius = 28 + MathF.Pow(1 - u, 1.8f) * (550 + Hash(index) * 420) * Scale;
        return Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * new Vector2(radius, radius * (Perfect ? .7f : .85f));
    }
    internal Vector2 Outflow(int index, float age)
    {
        float angle = Hash(index + 59) * MathHelper.TwoPi;
        float distance = Math.Max(0, age) * (4 + Hash(index + 3) * 8) + Math.Max(0, age) * Math.Max(0, age) * .032f;
        return Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * new Vector2(distance, distance * .78f);
    }
}
