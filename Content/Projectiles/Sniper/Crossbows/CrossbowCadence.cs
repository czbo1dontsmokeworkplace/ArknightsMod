using System;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

public enum CrossbowKind { Kroos = 0, KroosAlter = 1, Schwarz = 2, Pozemka = 3, Melanite = 4, Provence = 5, GreyThroat = 6, BluePoison = 7 }

public static class CrossbowBallistics
{
    public static float PiercingMultiplier(int priorHits) => MathF.Pow(.98f, Math.Max(0, priorHits));
    public static bool IsPiercing(CrossbowKind kind) => kind is CrossbowKind.Melanite or CrossbowKind.Provence or CrossbowKind.Schwarz;
    public static int BurstLength(CrossbowKind kind) => kind switch {
        CrossbowKind.BluePoison => 4, CrossbowKind.Pozemka => 1, _ => 3 };
}

// Gap means the time from the LAST arrow in a burst to the FIRST arrow of the next burst.
// Kept on the item, so releasing the mouse cannot bypass the recovery or Schwarz's 45 ticks.
public sealed class CrossbowCadence
{
    public double NextShotAt { get; private set; }
    public int ShotsInBurst { get; private set; }
    public bool Ready(ulong tick) => tick + .00001d >= NextShotAt;
    public void BeginForcedBurst(ulong tick)
    {
        ShotsInBurst = 0;
        NextShotAt = tick;
    }

    public void Fired(ulong tick, CrossbowKind kind, float speed = 1f, float skillInterval = 1f)
    {
        if (kind == CrossbowKind.Schwarz)
        {
            ShotsInBurst = 0;
            NextShotAt = tick + 45;
            return;
        }
        if (CrossbowBallistics.IsPiercing(kind))
        {
            ShotsInBurst = 0;
            NextShotAt = tick + Math.Max(1, (kind == CrossbowKind.Melanite ? 38 : 48) * skillInterval / Math.Max(.1f, speed));
            return;
        }
        // +15% fire rate shortens both the firing interval and burst recovery by 1 / 1.15.
        if (kind is CrossbowKind.GreyThroat or CrossbowKind.BluePoison) speed *= 1.15f;
        // Pozemka keeps the old seven-tick intra-burst interval continuously, including skills.
        if (kind == CrossbowKind.Pozemka)
        {
            ShotsInBurst = 0;
            double next = NextShotAt > tick - 1d ? NextShotAt : tick;
            NextShotAt = next + Math.Max(1d, 7d * skillInterval / Math.Max(.1f, speed));
            return;
        }
        bool last = ++ShotsInBurst == CrossbowBallistics.BurstLength(kind);
        double interval = last ? 30
            : (kind == CrossbowKind.Kroos ? 10 : 7);
        // Preserve fractional intervals at high attack speed instead of rounding every shot.
        double start = NextShotAt > tick - 1d ? NextShotAt : tick;
        NextShotAt = start + Math.Max(1d, interval * skillInterval / Math.Max(.1f, speed));
        if (last) ShotsInBurst = 0;
    }

    public void FiredKroosSkill(ulong tick, float speed, bool last)
    {
        // The skill is its own two-arrow burst, regardless of the normal burst position.
        double start = NextShotAt > tick - 1d ? NextShotAt : tick;
        NextShotAt = start + Math.Max(1d, (last ? 30d : 5d) / Math.Max(.1f, speed));
        ShotsInBurst = last ? 0 : 1;
    }

    public void Interrupt(ulong tick, CrossbowKind kind)
    {
        if (ShotsInBurst > 0)
            NextShotAt = Math.Max(NextShotAt, tick + 30d);
        ShotsInBurst = 0;
    }
}
