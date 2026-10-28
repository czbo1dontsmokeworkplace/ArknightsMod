using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;

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
        CoreCasterKind.Durin => 4, CoreCasterKind.Steward => 5, CoreCasterKind.Haze => 5,
        CoreCasterKind.Nightmare => 6, CoreCasterKind.Absinthe => 4,
        CoreCasterKind.Hoolheyak => 6, _ => 5
    };
    internal static float SpeedFor(CoreCasterKind kind) => kind switch {
        CoreCasterKind.Durin => 36, CoreCasterKind.Steward => 44, CoreCasterKind.Haze => 52,
        CoreCasterKind.Nightmare => 56, CoreCasterKind.Absinthe => 76,
        CoreCasterKind.Hoolheyak => 68, _ => 60
    };
    internal static int UpdatesFor(CoreCasterKind kind) => kind switch {
        CoreCasterKind.Durin => 4, CoreCasterKind.Steward => 4,
        CoreCasterKind.Absinthe => 8, CoreCasterKind.Hoolheyak => 6, _ => 6
    };
    // Keep each collision step short. Preserve real lifetime when increasing update count.
    // Spawn already contains authoritative velocity, so this deterministic conversion is safe on peers.
    internal static void Configure(Projectile p, CoreCasterKind kind, bool volcano = false)
    {
        if (volcano) return; // Keep the separately authored upward scatter / falling-shell sequence.
        int oldUpdates = p.MaxUpdates, updates = UpdatesFor(kind);
        p.timeLeft = (int)Math.Min(int.MaxValue, (long)p.timeLeft * updates / oldUpdates);
        p.extraUpdates = updates - 1;
        p.velocity = Unit(p.velocity) * SpeedFor(kind) / updates;
    }
    // Ordinary / empowered shots never acquire steering, including armor-provided homing.
    // Explicitly retained special attack forms keep their previous guidance.
    internal bool CanGuide(CoreCasterKind kind, int mode = 0) => kind == CoreCasterKind.Eyjafjalla && mode == 3
        ? Descending : (kind == CoreCasterKind.Absinthe && mode == 2 || kind == CoreCasterKind.Hoolheyak && mode >= 3)
            && Time > LaunchTicks(kind) + .0001f;
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
    internal void Step(CoreCasterKind kind, int mode, Vector2 center, ref Vector2 velocity, int updates, Vector2? target = null)
    {
        updates = Math.Max(1, updates);
        float dt = 1f / updates; Time += dt;
        if (kind == CoreCasterKind.Eyjafjalla && mode == 3) {
            volcano ??= new VolcanoFlight(); volcano.Step(center, ref velocity, updates, target ?? destination); return;
        }
        Vector2 forward = Unit(velocity);
        if (CanGuide(kind, mode) && target.HasValue)
        {
            guided = true;
            Vector2 offset = target.Value - center;
            float distance = offset.Length();
            float delta = MathHelper.WrapAngle(MathF.Atan2(offset.Y, offset.X) - MathF.Atan2(forward.Y, forward.X));
            // Fast launch followed by an actual curved turn, with no braking, wiggle or orbiting ball.
            // The physical turning radius contracts near the target, still divided across collision steps.
            float turn = Math.Clamp(speed / Math.Max(distance, 16) * 1.3f, .12f, 2.4f) * dt;
            float rotation = Math.Clamp(delta, -turn, turn);
            float c = MathF.Cos(rotation), s = MathF.Sin(rotation);
            forward = new(forward.X * c - forward.Y * s, forward.X * s + forward.Y * c);
        }
        velocity = forward * speed / updates;
    }
}
