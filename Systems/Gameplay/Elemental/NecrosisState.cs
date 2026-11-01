using System;

namespace ArknightsMod.Systems.Gameplay.Elemental;

internal sealed class NecrosisState
{
    internal const int Threshold = 1000, Duration = 15 * 60;
    internal int Accumulation { get; private set; }
    internal int Remaining { get; private set; }
    internal bool Active => Remaining > 0;
    internal bool Add(int amount) {
        if (amount <= 0 || Active) return false;
        Accumulation = (int)Math.Min(Threshold, (long)Accumulation + amount);
        if (Accumulation == Threshold) { Accumulation = 0; Remaining = Duration; }
        return true;
    }
    // True once per elapsed second, including the final tick of the burst.
    internal bool Tick() {
        if (!Active) return false;
        Remaining--;
        return Remaining % 60 == 0;
    }
    internal void Set(int accumulation, int remaining) {
        Remaining = Math.Clamp(remaining, 0, Duration);
        Accumulation = Active ? 0 : Math.Clamp(accumulation, 0, Threshold - 1);
    }
    internal void Clear() => Set(0, 0);
}
