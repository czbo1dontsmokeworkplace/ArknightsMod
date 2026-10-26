using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// Launch randomness is supplied once by the owner. Never read a local cursor/random source in flight.
internal sealed class VolcanoFlight
{
    internal float Age { get; private set; }
    internal bool Descending { get; private set; }
    internal void Restore(float age, bool descending) { Age = Math.Clamp(age, 0, 160); Descending = descending; }
    internal void Step(Vector2 center, ref Vector2 velocity, int updates, Vector2 strikePoint)
    {
        float dt = 1f / updates; Age += dt;
        Vector2 physical = velocity * updates;
        if (!Descending)
        {
            physical.X *= MathF.Pow(.992f, dt);
            physical.Y += .82f * dt;
            if (Age >= 16 && physical.Y >= 0 || Age >= 46)
            {
                Descending = true;
                // Keep the launch velocity continuous through the apex. Guidance accelerates
                // into the descent instead of snapping the shell and its trail to a new heading.
                physical.Y = Math.Max(.25f, physical.Y);
            }
        }
        else
        {
            Vector2 offset = strikePoint - center;
            float distance = offset.Length();
            Vector2 desired = CoreCasterFlight.Unit(offset);
            // It remains a falling shell: no turning back up into a homing orbit.
            desired.Y = Math.Max(.25f, desired.Y);
            desired = CoreCasterFlight.Unit(desired);
            float speed = Math.Clamp(physical.Length() + 7.2f, 15, 48);
            if (distance < 120) speed = Math.Min(speed, Math.Max(10, distance * .38f));
            physical = Vector2.Lerp(physical, desired * speed, 1 - MathF.Pow(distance < 140 ? .68f : .84f, dt));
            physical.Y = Math.Max(2, physical.Y);
        }
        velocity = physical / updates;
    }
}
