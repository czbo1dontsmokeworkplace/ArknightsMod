using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class ArsenalBeamGeometry
{
    // Slab intersection gives a beam's first contact, including wide/tall targets.
    internal static bool Entry(Vector2 origin, Vector2 axis, Rectangle box, float range, float padding, out float entry)
    {
        float near = 0, far = range;
        bool Slab(float at, float direction, float min, float max) {
            if (MathF.Abs(direction) < .00001f) return at >= min && at <= max;
            float a = (min - at) / direction, b = (max - at) / direction;
            near = Math.Max(near, Math.Min(a, b)); far = Math.Min(far, Math.Max(a, b));
            return near <= far;
        }
        bool hit = Slab(origin.X, axis.X, box.Left - padding, box.Right + padding)
            && Slab(origin.Y, axis.Y, box.Top - padding, box.Bottom + padding);
        entry = near; return hit;
    }
}
