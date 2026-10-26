using System;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

// NukeOfBliss's AI-update counts: three updates outbound, eleven on the final dive.
internal sealed class BlissfulMortarFlight
{
    internal int Time, Rain = 150;
    internal bool Diving => Time > 120 && Rain == 0;
    internal bool Staging => Time > 120 && Rain > 1;
    internal float Opacity => Rain <= 0 ? 1 : Math.Clamp((120 - Time) / 36f, 0, 1);
    internal bool Step(ref Vector2 center, ref Vector2 velocity, Vector2 mouse, Vector2 owner, Vector2? target, float gravity = 1)
    {
        bool transfer = false;
        if (Time > 120)
        {
            if (Rain > 1) { center = new Vector2(mouse.X, owner.Y - 600 * gravity); transfer = true; }
            if (Rain > 0) Rain--;
            if (Rain == 1) velocity = Unit(mouse - center) * 15;
            if (Rain == 0 && target.HasValue && (target.Value.Y - center.Y) * gravity > 0)
            {
                Vector2 direction = Unit(target.Value - center);
                if (velocity.Length() < 15) velocity = velocity * .98f + direction * 2.5f;
                else velocity *= .9f;
            }
        }
        else velocity *= .995f;
        Time++; return transfer;
    }
    private static Vector2 Unit(Vector2 v) => v.LengthSquared() > .001f ? Vector2.Normalize(v) : Vector2.UnitY;
}
