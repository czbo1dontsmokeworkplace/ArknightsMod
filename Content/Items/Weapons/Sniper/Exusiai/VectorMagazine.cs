using System;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;

// Real game ticks, independent of item attack speed and projectile extraUpdates.
public sealed class VectorMagazine
{
    public const int Capacity = 40, Interval = 3, ReloadDuration = 72;
    public const int EmpoweredShotLimit = Capacity * 3 / 10;
    public int Rounds { get; private set; } = Capacity;
    public int ReloadTicks { get; private set; }
    public int Cooldown { get; private set; }
    public int EmpoweredShots { get; private set; }
    public bool Reloading => ReloadTicks > 0;
    public float Progress => Reloading ? 1f - ReloadTicks / (float)ReloadDuration : Rounds / (float)Capacity;

    public void Restore(int rounds, int reload, int empowered)
    {
        Rounds = Math.Clamp(rounds, 0, Capacity);
        ReloadTicks = Rounds == 0 ? Math.Clamp(reload, 1, ReloadDuration) : 0;
        EmpoweredShots = Math.Clamp(empowered, 0, EmpoweredShotLimit);
        Cooldown = 0;
    }
    public VectorMagazine Copy()
    {
        var copy = new VectorMagazine();
        copy.Restore(Rounds, ReloadTicks, EmpoweredShots);
        copy.Cooldown = Cooldown;
        return copy;
    }
    public bool Tick()
    {
        // Preserve the last shot's interval before starting the reload clock.
        if (Cooldown > 0) { Cooldown--; return false; }
        if (!Reloading) return false;
        if (--ReloadTicks > 0) return false;
        Rounds = Capacity;
        return true;
    }
    public bool CanFire => !Reloading && Rounds > 0 && Cooldown == 0;
    public bool Fire()
    {
        if (!CanFire) return false;
        Rounds--;
        if (EmpoweredShots > 0) EmpoweredShots--;
        Cooldown = Interval;
        if (Rounds == 0)
        {
            ReloadTicks = ReloadDuration;
        }
        return true;
    }
    public void Empower()
    {
        if (Reloading) return;
        // S1 enhances a fixed number of trigger pulls; it must never refill the magazine.
        EmpoweredShots = EmpoweredShotLimit;
    }
    public void ClearEmpowerment() => EmpoweredShots = 0;
}

public static class VectorBalance
{
    public const int PanelDamage = 26;
    public const float TargetRatio = 1.5f;
    public static int Lanes(int mode) => mode == 3 ? 3 : mode is 1 or 2 ? 2 : 1;
    // SHPC-style S1: the original main chain plus one close parallel side chain.
    public static float LaneOffset(int mode, int index) => mode == 1 ? index * 6f : (index - (Lanes(mode) - 1) / 2f) * 10f;
    public static float ShotsPerSecond => 60f * VectorMagazine.Capacity /
        (VectorMagazine.Capacity * VectorMagazine.Interval + VectorMagazine.ReloadDuration);

    // Legacy: 15 animation + 15 reuse; S2 has 4 shots/30 ticks;
    // S3 has 4 shots/(8 animation + 15 reuse). S1 cycles 4 singles + one triple burst.
    // The original rework's per-projectile damage is retained. S1's 12-shot/two-chain
    // duration/count reduction intentionally lowers its total effect; do not compensate it here.
    public static float ExactDamage(float legacyBase, float ammo, int mode)
    {
        float normal = legacyBase + ammo;
        return mode switch {
            1 => .056f * normal + .072f * (legacyBase * 1.45f + ammo),
            2 => TargetRatio * 8f / (ShotsPerSecond * 2) * (legacyBase * 1.25f + ammo),
            3 => TargetRatio * (240f / 23f) / (ShotsPerSecond * 3) * (legacyBase * 1.1f + ammo),
            _ => TargetRatio * 2f / ShotsPerSecond * normal
        };
    }
    public static int Damage(float legacyBase, float ammo, int mode) => Math.Max(1, (int)MathF.Ceiling(ExactDamage(legacyBase, ammo, mode)));
}
