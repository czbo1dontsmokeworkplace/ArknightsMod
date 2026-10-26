using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.VisualEffects.HelldiverExplosions;

// ExplosionBurst/Update/Draw from the source mod, restricted to the six explosion particle kinds.
// Its complete one-shot event keeps all lobes, delayed smoke, debris and sparks together.
internal sealed class HelldiverExplosionEmitter
{
    internal enum Kind { Spark, Smoke, Plasma, Flame, Shock, Debris }
    private sealed class Mote
    {
        internal Kind Kind; internal Vector2 Position, Velocity, Stretch = Vector2.One;
        internal Color Color; internal float Size, Age, Life, Rotation, Gravity, Spin, Growth, Opacity = 1, Drag = .96f;
        internal int Frame, Delay;
    }
    private readonly List<Mote> particles = new();
    private readonly ExplosionStyle style;
    private readonly Color theme;
    private readonly float radius;
    private readonly int seed;
    internal int Age { get; private set; }
    internal int Count => particles.Count;
    internal HelldiverExplosionEmitter(float radius, ExplosionStyle style, Color color, int seed)
    {
        this.radius = radius; this.style = style; theme = color; this.seed = seed;
        var rng = new Random(seed);
        float R(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);
        Vector2 Dir() => R(0, MathHelper.TwoPi).ToRotationVector2();
        void Add(Kind kind, Vector2 p, Vector2 v, Color c, float size, int life, float rotation = 0,
            Vector2? stretch = null, float gravity = 0, float spin = 0, int frame = 0, float growth = 0,
            float opacity = 1, float drag = .96f, int delay = 0)
            => particles.Add(new Mote { Kind = kind, Position = p, Velocity = v, Color = c, Size = size, Life = life,
                Rotation = rotation, Stretch = stretch ?? Vector2.One, Gravity = gravity, Spin = spin, Frame = frame,
                Growth = growth, Opacity = opacity, Drag = drag, Delay = delay });
        int sparks = Math.Clamp((int)(radius * .32f), 28, 130); float power = Math.Clamp(radius / 100, .7f, 3);
        for (int i = 0; i < sparks; i++)
        {
            Vector2 dir = Dir(); float speed = R(2, 11) * power;
            Add(Kind.Spark, dir * radius * .08f, dir * speed, i % 5 == 0 ? Color.White : color,
                R(10, 22), rng.Next(20, 48), stretch: new(2, .48f), gravity: .14f, drag: .94f);
        }
        Add(Kind.Shock, Vector2.Zero, Vector2.Zero, color, radius * .7f, 18, growth: radius * .075f, opacity: .7f);
        for (int i = 0; i < Math.Clamp((int)(radius * .13f), 10, 38); i++)
        {
            Vector2 dir = Dir(); Vector2 q = dir * radius * R(0, .3f);
            Add(style == ExplosionStyle.Plasma ? Kind.Plasma : Kind.Flame, q, dir * R(1, 4) * power,
                color, R(25, 55) * power, rng.Next(17, 32), dir.ToRotation(), spin: R(-.035f, .035f), frame: i, growth: .5f, opacity: .75f);
        }
        for (int i = 0; i < Math.Clamp((int)(radius * .08f), 8, 24); i++)
        {
            Vector2 dir = Dir();
            Add(Kind.Smoke, dir * radius * .17f, dir * R(1, 4) * power - Vector2.UnitY, new Color(43, 41, 42),
                R(35, 70) * power, rng.Next(40, 76), spin: R(-.025f, .025f), frame: i, growth: 1.1f, opacity: .42f, delay: 4 + i % 7);
            Add(Kind.Debris, Vector2.Zero, dir * R(3, 8) * power, Color.Lerp(new Color(96, 91, 88), color, .35f),
                R(5, 11), rng.Next(24, 55), gravity: .25f, spin: R(-.2f, .2f));
        }
    }
    internal void Update()
    {
        Age++;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i]; if (p.Delay > 0) { p.Delay--; continue; }
            p.Age++; p.Position += p.Velocity; p.Velocity *= p.Drag; p.Velocity.Y += p.Gravity;
            p.Rotation += p.Spin; p.Size = Math.Max(.1f, p.Size + p.Growth);
            if (p.Kind == Kind.Smoke) p.Velocity.Y -= .035f;
            if (p.Kind == Kind.Flame) p.Velocity.Y -= .018f;
            if (p.Kind is Kind.Spark or Kind.Debris) p.Rotation = p.Velocity.ToRotation();
            if (p.Age >= p.Life) particles.RemoveAt(i);
        }
    }
    internal void Draw(SpriteBatch b, Vector2 at)
    {
        float life = style == ExplosionStyle.Nuclear ? 65 : 28;
        if (Age <= life) HelldiverExplosionArt.Explosion(b, at, radius, Age / life, style, seed, theme);
        foreach (var p in particles)
            if (p.Delay == 0 && p.Kind == Kind.Smoke)
                HelldiverExplosionArt.SmokePuff(b, at + p.Position, p.Size, p.Age / p.Life, p.Color, p.Frame, p.Rotation, p.Opacity);
        foreach (var p in particles)
        {
            if (p.Delay > 0) continue; float t = p.Age / p.Life, fade = 1 - t * t * t; Vector2 pos = at + p.Position;
            if (p.Kind == Kind.Spark) HelldiverExplosionArt.Glow(b, pos, p.Size * 1.3f, p.Color, fade * .12f);
            else if (p.Kind == Kind.Plasma)
            { HelldiverExplosionArt.Glow(b, pos, p.Size * fade * 1.6f, p.Color, .33f); HelldiverExplosionArt.Glow(b, pos, p.Size * fade * .38f, Color.White, .9f); }
            else if (p.Kind == Kind.Flame) HelldiverExplosionArt.Glow(b, pos, p.Size * .9f, p.Color, MathF.Pow(1 - t, .7f) * .1f * p.Opacity);
        }
        foreach (Kind kind in Enum.GetValues<Kind>()) foreach (var p in particles)
        {
            if (p.Delay > 0 || p.Kind != kind || kind == Kind.Smoke) continue;
            float t = p.Age / p.Life, fade = 1 - t * t * t; Vector2 pos = at + p.Position;
            Color c = HelldiverExplosionArt.Add(p.Color, fade * p.Opacity);
            switch (kind)
            {
                case Kind.Flame: HelldiverExplosionArt.Flame(b, pos, p.Rotation, t, p.Size, p.Frame, theme, false, p.Opacity * .52f); break;
                case Kind.Plasma: HelldiverExplosionArt.Plasma(b, pos, p.Rotation.ToRotationVector2(), (int)p.Age, p.Color, p.Size * fade, p.Frame, false); break;
                case Kind.Shock: b.Draw(HelldiverExplosionArt.Shock, pos, null, c * (1 - t), p.Rotation, HelldiverExplosionArt.Shock.Size() / 2, p.Size / HelldiverExplosionArt.Shock.Width, 0, 0); break;
                case Kind.Debris: b.Draw(HelldiverExplosionArt.Chip, pos, null, p.Color * fade, p.Rotation, HelldiverExplosionArt.Chip.Size() / 2, p.Size / HelldiverExplosionArt.Chip.Width, 0, 0); break;
                default:
                    var texture = HelldiverExplosionArt.Streak; Vector2 stretch = p.Stretch * p.Size / texture.Width;
                    b.Draw(texture, pos, null, c, p.Rotation, texture.Size() / 2, stretch * MathF.Sqrt(Math.Max(0, 1 - t)), 0, 0);
                    b.Draw(texture, pos, null, HelldiverExplosionArt.Add(Color.Lerp(p.Color, Color.White, .8f), fade * p.Opacity * .8f), p.Rotation, texture.Size() / 2, stretch * new Vector2(.9f, .36f), 0, 0);
                    break;
            }
        }
    }
}
