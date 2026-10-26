using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// Anchor's coast -> gravity -> recall, measured in real ticks at every update rate.
internal sealed class UlpianusThrowFlight
{
    internal float Age { get; private set; }
    internal bool Returning { get; private set; }
    internal void Recall() => Returning = true;
    internal void Restore(float age, bool returning) { Age = Math.Clamp(age, 0, 200); Returning = returning; }
    internal void Step(Vector2 center, Vector2 owner, ref Vector2 velocity, int updates, float charge)
    {
        float dt = 1f / updates; Age += dt;
        Vector2 physical = velocity * updates;
        if (Vector2.Distance(center, owner) > MathHelper.Lerp(800, 1050, charge) || Age >= 66) Returning = true;
        if (Returning)
        {
            Vector2 offset = owner - center;
            Vector2 desired = offset.LengthSquared() > .001f ? Vector2.Normalize(offset) * MathHelper.Lerp(24, 34, charge) : Vector2.Zero;
            Vector2 change = desired - physical;
            if (change.LengthSquared() > .001f) physical += Vector2.Normalize(change) * Math.Min(4 * dt, change.Length());
        }
        else if (Age >= 10)
        {
            physical.Y += (.5f + (physical.Y < 0 ? .35f : 0)) * dt;
            physical.Y = Math.Min(16, physical.Y);
            physical.X *= MathF.Pow(.95f, dt);
        }
        velocity = physical / updates;
    }
}
