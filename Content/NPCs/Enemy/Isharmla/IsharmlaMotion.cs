using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

// Movement changes velocity, never the worm's recorded position. Portals own the only route discontinuities.
internal static class IsharmlaMotion
{
    internal static Vector2 Swim(Vector2 velocity, Vector2 heading, Vector2 offset, float speed, float turn)
    {
        float distance = offset.Length();
        float current = velocity.Length();
        float angle = (current > .35f ? velocity : heading).ToRotation();
        float goal = offset.SafeNormalize(heading).ToRotation();
        float error = Math.Abs(MathHelper.WrapAngle(goal - angle));
        // Broad turns shed speed; a straightened body can build speed again, like Infernum's flying worms.
        float alignment = IsharmlaRules.Smooth((error - .45f) / 1.85f);
        float braking = .75f + speed * .012f;
        float arrival = Math.Min(distance * .085f, MathF.Sqrt(2 * braking * distance));
        float target = Math.Min(speed * MathHelper.Lerp(1, .34f, alignment), arrival);
        float steering = turn * MathHelper.Lerp(1.55f, .78f, MathHelper.Clamp(current / speed, 0, 1));
        // Keep the turning radius inside the remaining approach. Otherwise a slow orbit can sustain itself forever.
        if (error > .35f) target = Math.Min(target, distance * steering * .55f);
        float next = Approach(current, target, target > current ? .28f + speed * .012f : braking);
        if (distance < 12 && current < 1) return velocity * .78f;
        return angle.AngleTowards(goal, steering).ToRotationVector2() * next;
    }

    internal static Vector2 Hover(Vector2 velocity, Vector2 offset, float response, float speed)
    {
        // A critically damped spring has a soft arrival without circling or crossing the hovering point.
        float frequency = MathHelper.Clamp(.055f + response * .38f, .07f, .115f);
        float reach = speed * 2 / frequency;
        if (offset.LengthSquared() > reach * reach) offset = offset.SafeNormalize(Vector2.UnitY) * reach;
        Vector2 acceleration = offset * (frequency * frequency) - velocity * (2 * frequency);
        float limit = .65f + speed * .035f;
        if (acceleration.LengthSquared() > limit * limit) acceleration = acceleration.SafeNormalize(Vector2.UnitY) * limit;
        Vector2 result = velocity + acceleration;
        if (velocity.LengthSquared() <= speed * speed && result.LengthSquared() > speed * speed)
            result = result.SafeNormalize(Vector2.UnitY) * speed;
        return result;
    }

    internal static Vector2 Rush(Vector2 velocity, Vector2 direction, float peak, int age, int duration, float turn = .14f)
    {
        direction = direction.SafeNormalize(Vector2.UnitX);
        bool light = duration < 30;
        float launchTime = light ? 3 : 14, releaseTime = light ? 5 : 18;
        float launch = IsharmlaRules.Smooth(age / launchTime);
        float release = IsharmlaRules.Smooth((age - duration + releaseTime) / releaseTime);
        float target = peak * MathHelper.Lerp(.38f, 1, launch) * MathHelper.Lerp(1, .60f, release);
        float current = velocity.Length();
        float heading = current > .35f ? velocity.ToRotation() : direction.ToRotation();
        float speed = Approach(current, target, peak * (light ? (target > current ? .20f : .10f) : (target > current ? .065f : .05f)));
        return heading.AngleTowards(direction.ToRotation(), turn).ToRotationVector2() * speed;
    }

    internal static Vector2 LineUp(Vector2 velocity, Vector2 direction, float speed)
    {
        direction = direction.SafeNormalize(Vector2.UnitY);
        float current = velocity.Length();
        float heading = current > .35f ? velocity.ToRotation() : direction.ToRotation();
        return heading.AngleTowards(direction.ToRotation(), .135f).ToRotationVector2() * Approach(current, speed, .95f);
    }

    internal static Vector2 Brake(Vector2 velocity, float drag, float deceleration)
    {
        float speed = velocity.Length();
        return speed < .03f ? Vector2.Zero : velocity / speed * Math.Max(0, speed - Math.Min(deceleration, speed * drag));
    }

    private static float Approach(float current, float target, float amount)
        => current + MathHelper.Clamp(target - current, -amount, amount);
}
