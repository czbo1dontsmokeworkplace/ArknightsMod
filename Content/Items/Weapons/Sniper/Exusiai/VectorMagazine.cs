using System;
using Microsoft.Xna.Framework;

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
    public const int PanelDamage = 64;
    public const int InnateArmorPenetration = 35;
    public const float TargetRatio = 1.5f;
    public static int Lanes(int mode) => mode == 3 ? 3 : mode is 1 or 2 ? 2 : 1;
    // SHPC: centered main shot, close secondary origins and small angular divergence.
    public static float LaneOffset(int mode, int index) => index switch { 1=>2.5f, 2=>-4f, _=>0 };
    public static float LaneAngle(int mode,int index,float sample)
    {
        if(index==0)return 0;
        sample=Math.Clamp(sample,-1,1);
        float sign=Lanes(mode)==3?(index==1?1:-1):(sample<0?-1:1);
        return sign*MathHelper.ToRadians(MathHelper.Lerp(.45f,2.2f,Math.Abs(sample)));
    }
    public static float ShotsPerSecond => 60f * VectorMagazine.Capacity /
        (VectorMagazine.Capacity * VectorMagazine.Interval + VectorMagazine.ReloadDuration);

    // Direct per-shot damage, including ammo and the real player modifiers.
    // High rate of fire no longer divides every bullet below enemy defense.
    public static float ExactDamage(float weapon, float ammo, int mode)
    {
        float multiplier=mode switch {1=>1.16f,2=>1.22f,3=>1.30f,_=>1};
        return weapon*multiplier+ammo;
    }
    public static int Damage(float legacyBase, float ammo, int mode) => Math.Max(1, (int)MathF.Ceiling(ExactDamage(legacyBase, ammo, mode)));
}
