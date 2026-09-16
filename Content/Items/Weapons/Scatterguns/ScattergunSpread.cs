using System;

namespace ArknightsMod.Content.Items.Weapons.Scatterguns;

public static class ScattergunSpread
{
    // Full cone angle. Movement affects direction only, never projectile damage or cadence.
    public static float TotalDegrees(float movementSpeed, float fullRunSpeed)
    {
        if (!float.IsFinite(movementSpeed)) return 20f;
        if (!float.IsFinite(fullRunSpeed)) fullRunSpeed = 3f;
        float fraction = Math.Clamp(Math.Abs(movementSpeed) / Math.Max(1f, fullRunSpeed), 0f, 1f);
        return 5f + 15f * fraction;
    }
}
