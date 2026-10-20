using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static class MaterialistFlight
{
    internal static Vector2 Limit(Vector2 value, float maximum)
    {
        float length = value.Length();
        return length > maximum && length > .001f ? value * (maximum / length) : value;
    }
    internal static Vector2 Approach(Vector2 current, Vector2 desired, float acceleration) => current + Limit(desired - current, acceleration);
    internal static Vector2 Follow(Vector2 position, Vector2 velocity, Vector2 destination, Vector2 feedForward,
        float baseSpeed, float targetSpeed, float acceleration, float maximumSpeed, float gain = .09f)
    {
        Vector2 error = destination - position;
        float correctionSpeed = baseSpeed + MathHelper.Clamp((error.Length() - 180) * .018f, 0, 16);
        Vector2 desired = feedForward + Limit(error * gain, correctionSpeed);
        float cap = Math.Min(maximumSpeed, baseSpeed + targetSpeed * 1.12f + MathHelper.Clamp(error.Length() * .008f, 0, 18));
        return Approach(velocity, Limit(desired * MaterialistRules.Mobility, cap * MaterialistRules.Mobility), acceleration * MaterialistRules.Mobility);
    }
}
