using System;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

public enum CrossbowKind { Kroos, KroosAlter, Schwarz, Pozemka }

public static class CrossbowBallistics
{
    public static float PiercingMultiplier(int priorHits) => MathF.Pow(.98f, Math.Max(0, priorHits));
}

// Gap means the time from the LAST arrow in a burst to the FIRST arrow of the next burst.
// Kept on the item, so releasing the mouse cannot bypass the recovery or Schwarz's 45 ticks.
public sealed class CrossbowCadence
{
    public double NextShotAt { get; private set; }
    public int ShotsInBurst { get; private set; }
    public bool Ready(ulong tick) => tick + .00001d >= NextShotAt;

    public void Fired(ulong tick, CrossbowKind kind, float speed = 1f, float skillInterval = 1f)
    {
        if (kind == CrossbowKind.Schwarz)
        {
            ShotsInBurst = 0;
            NextShotAt = tick + 45;
            return;
        }
        bool last = ++ShotsInBurst == 3;
        double interval = last ? (kind == CrossbowKind.Pozemka ? 21 : 30)
            : (kind == CrossbowKind.Kroos ? 10 : 7);
        // Preserve fractional intervals at high attack speed instead of rounding every shot.
        double start = NextShotAt > tick - 1d ? NextShotAt : tick;
        NextShotAt = start + Math.Max(1d, interval * skillInterval / Math.Max(.1f, speed));
        if (last) ShotsInBurst = 0;
    }

    public void Interrupt(ulong tick, CrossbowKind kind)
    {
        if (ShotsInBurst > 0)
            NextShotAt = Math.Max(NextShotAt, tick + (kind == CrossbowKind.Pozemka ? 21d : 30d));
        ShotsInBurst = 0;
    }
}
