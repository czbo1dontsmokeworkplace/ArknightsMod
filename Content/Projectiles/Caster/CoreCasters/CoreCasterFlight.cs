using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// Real game ticks, independent of extraUpdates. Launch data and guidance age travel together.
internal sealed class CoreCasterFlight
{
    private Vector2 direction, side, destination, origin;
    private float speed, sign;
    private bool guided;
    private VolcanoFlight volcano;
    internal float Time { get; private set; }
    internal bool Descending => volcano?.Descending == true;
    internal static float LaunchTicks(CoreCasterKind kind) => kind switch {
        CoreCasterKind.Durin => 9, CoreCasterKind.Steward => 15, CoreCasterKind.Haze => 17,
        CoreCasterKind.Nightmare => 20, CoreCasterKind.Absinthe => 14,
        CoreCasterKind.Hoolheyak => 18, _ => 16
    };
    internal bool CanGuide(CoreCasterKind kind, int mode = 0) => kind == CoreCasterKind.Eyjafjalla && mode == 3
        ? Descending : Time > LaunchTicks(kind) + .0001f;
    internal CoreCasterFlight(Vector2 center, Vector2 velocity, int updates, int seed, Vector2? aim = null)
    {
        origin = center; direction = Unit(velocity); side = new(-direction.Y, direction.X);
        speed = Math.Max(1, velocity.Length() * updates); sign = (seed & 1) == 0 ? 1 : -1;
        destination = aim ?? center + direction * 720;
    }
    internal void Write(BinaryWriter w)
    {
        w.Write(origin.X); w.Write(origin.Y); w.Write(direction.X); w.Write(direction.Y);
        w.Write(destination.X); w.Write(destination.Y); w.Write(speed); w.Write(sign);
        w.Write(Time); w.Write(guided); w.Write(Descending);
    }
    internal static CoreCasterFlight Read(BinaryReader r)
    {
        var f = new CoreCasterFlight(Vector2.Zero, Vector2.UnitX, 1, 0) {
            origin = new(r.ReadSingle(), r.ReadSingle()), direction = new(r.ReadSingle(), r.ReadSingle()),
            destination = new(r.ReadSingle(), r.ReadSingle()), speed = r.ReadSingle(), sign = r.ReadSingle(),
            Time = r.ReadSingle(), guided = r.ReadBoolean()
        };
        f.side = new(-f.direction.Y, f.direction.X);
        bool descending = r.ReadBoolean();
        f.volcano = new VolcanoFlight(); f.volcano.Restore(f.Time, descending);
        return f;
    }
    internal static Vector2 Unit(Vector2 v) => v.LengthSquared() > .0001f ? Vector2.Normalize(v) : Vector2.UnitX;
    private static Vector2 Rotate(Vector2 v, float a) => new(v.X * MathF.Cos(a) - v.Y * MathF.Sin(a), v.X * MathF.Sin(a) + v.Y * MathF.Cos(a));
    private float Wave(float t, float amplitude, float frequency) => sign * amplitude * MathF.Sin(t * frequency);
    internal void Step(CoreCasterKind kind, int mode, Vector2 center, ref Vector2 velocity, int updates, Vector2? target = null)
    {
        updates = Math.Max(1, updates);
        float dt = 1f / updates, previous = Time; Time += dt;
        if (kind == CoreCasterKind.Eyjafjalla && mode == 3) {
            volcano ??= new VolcanoFlight(); volcano.Step(center, ref velocity, updates, target ?? destination); return;
        }
        float launch = LaunchTicks(kind);
        Vector2 physical = velocity * updates;
        if (Time <= launch + .0001f)
        {
            // A target cannot bend the independent launch gesture.
            switch (kind) {
                case CoreCasterKind.Durin:
                    physical = direction * speed + side * ((Wave(Time, 3, .28f) - Wave(previous, 3, .28f)) / dt); break;
                case CoreCasterKind.Steward:
                    physical = Rotate(direction, sign * .24f * MathF.Sin(Time / launch * MathHelper.TwoPi)) * speed; break;
                case CoreCasterKind.Haze:
                    physical = direction * speed + side * ((Wave(Time, 9, .22f) - Wave(previous, 9, .22f)) / dt); break;
                case CoreCasterKind.Nightmare:
                    physical = Rotate(direction, sign * .38f * MathF.Sin(Time / launch * MathHelper.TwoPi)) * speed; break;
                case CoreCasterKind.Absinthe:
                    physical = direction * speed * (.85f + .15f * Time / launch); break;
                case CoreCasterKind.Hoolheyak:
                    physical = direction * speed + side * ((Wave(Time, 13, .19f) - Wave(previous, 13, .19f)) / dt); break;
                default:
                    physical = Ballistic(Time, dt, mode); break;
            }
            // Last four ticks settle gently, adapting PurifiedGel_Ball's slowdown stage.
            float settle = Math.Clamp((Time - launch + 4) / 4, 0, 1);
            physical *= 1 - settle * .12f;
        }
        else if (target.HasValue)
        {
            guided = true;
            Vector2 offset = target.Value - center;
            float distance = offset.Length();
            float warmup = Math.Clamp((Time - launch) / 18, 0, 1);
            float close = 1 - Math.Clamp((distance - 55) / 260, 0, 1);
            float pull = Math.Max(warmup, close * .8f);
            float pace = Math.Clamp(speed * (1 + .18f * warmup), 8, kind == CoreCasterKind.Absinthe ? 34 : 28);
            // Tighten the turn near small enemies instead of orbiting past their hitboxes.
            pace = Math.Min(pace, Math.Max(5, distance * .32f));
            Vector2 desired = Unit(offset) * pace;
            physical = Vector2.Lerp(physical, desired, 1 - MathF.Pow(1 - (.075f + .19f * pull), dt));
            if (physical.LengthSquared() < 9) physical += Unit(offset) * 1.5f * dt;
        }
        else if (!guided && kind == CoreCasterKind.Eyjafjalla)
            physical = Ballistic(Time, dt, mode);
        // Losing a target never resets the launch gesture or the guidance clock.
        velocity = physical / updates;
    }
    private Vector2 Ballistic(float time, float dt, int mode)
    {
        float gravity = mode == 2 ? .72f : .46f;
        float duration = Math.Clamp(Vector2.Distance(origin, destination) / speed, 12, 55);
        Vector2 launch = (destination - origin) / duration - Vector2.UnitY * (gravity * duration * .5f);
        return launch + Vector2.UnitY * (gravity * (time - dt * .5f));
    }
}
