using System;
using System.Collections.Generic;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// CLCBLightingBoltsSystem reference: flower paths, geometric bursts and constellation
// stars. Local implementation; PrettySparkle, Dust and Arknights particles have
// separate registration paths. No CLCB/Calamity runtime calls or assets.
internal static class MostimaGrandClockVfx
{
    internal const int DecorationCount = 18;
    private static readonly ParticlePool<PrettySparkleParticle> Pool = new(256, () => new());
    private static ulong budgetFrame;
    private static int lights, dusts;
    private static bool Permit(Vector2 center, bool dust)
    {
        if (!SpreadVfxBudget.Visible(center, 540f)) return false;
        if (budgetFrame != Main.GameUpdateCount) { budgetFrame = Main.GameUpdateCount; lights = dusts = 0; }
        return dust ? dusts++ < 48 : lights++ < 144;
    }
    internal static Vector2 CrownPoint(int spoke, int point, float phase)
    {
        float t = point / 3f;
        float a = spoke * MathHelper.TwoPi / DecorationCount + phase + MathF.Sin(t * MathHelper.Pi) * .11f;
        return a.ToRotationVector2() * MathHelper.Lerp(232f, 345f, t);
    }
    private static void Light(Vector2 center, Vector2 offset, Color tint, float angle, Vector2 scale, int life, float speed = .3f)
    {
        if (!Permit(center, false)) return;
        PrettySparkleParticle p = Pool.RequestParticle();
        p.LocalPosition = center + offset;
        p.Velocity = offset.SafeNormalize(Vector2.UnitX) * speed;
        p.Rotation = angle; p.ColorTint = tint;
        p.Scale = scale;
        p.TimeToLive = life;
        p.FadeInNormalizedTime = .04f; p.FadeOutNormalizedTime = .8f;
        // PrettySparkle.Draw evaluates these values on a normalized 0..60 timeline.
        p.FadeInEnd = 6; p.FadeOutStart = 24; p.FadeOutEnd = 60;
        p.AdditiveAmount = .7f;
        Main.ParticleSystem_World_OverPlayers.Add(p);
    }
    internal static void Tick(Vector2 center, int age, float scale = 1f, float gold = 0f, bool beatArrived = false)
    {
        if (!SpreadVfxBudget.Visible(center, 720f)) return;
        if (age % 3 == 0)
            for (int i = 0; i < 3; i++)
                if (SpreadVfxBudget.Take(center, 720f))
                    new MostimaTimeTransit(center, age * .37f + i * MathHelper.TwoPi / 3f, gold).Spawn();
        if (age < 12) return;
        int tick = (age - 12) % MostimaClockwork.BeatTicks, beat = age / MostimaClockwork.BeatTicks;
        float phase = beat * .13f;
        if (beatArrived || tick == 0 && scale <= 1.001f)
        {
            for (int i = 0; i < DecorationCount; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Vector2 offset = CrownPoint(i, j, phase) * scale;
                    Light(center, offset, MostimaClockwork.TimeColor(j == 3 ? MostimaClockwork.Brass : MostimaVisuals.Blue, gold),
                        offset.ToRotation() + MathHelper.PiOver2, new Vector2(j == 3 ? 1.15f : .6f, .28f), 16);
                }
                for (int kind = 0; kind < 3; kind++)
                    if (SpreadVfxBudget.Take(center, 440f))
                        new MostimaRitualParticle(center, i * MathHelper.TwoPi / DecorationCount + phase, kind, i % 2 == 0, scale, gold).Spawn();
            }
        }
        if (tick == 6)
            for (int i = 0; i < DecorationCount; i++)
            {
                Vector2 offset = (i * MathHelper.TwoPi / DecorationCount - phase).ToRotationVector2() * 315f * scale;
                for (int axis = 0; axis < 2; axis++)
                    Light(center, offset, MostimaClockwork.TimeColor(i % 3 == 0 ? MostimaClockwork.Brass : MostimaVisuals.Violet, gold),
                        phase + axis * MathHelper.PiOver2, new Vector2(.95f, .2f), 12);
            }
        // Dust is emitted once at each selected phase; it is not a mod Particle.
        if (beatArrived || tick is 6 or 9)
            for (int i = 0; i < (beatArrived ? 24 : 12); i++)
            {
                if (!Permit(center, true) || MostimaClockDustSystem.Count >= 256) break;
                float a = i * MathHelper.TwoPi / (beatArrived ? 24f : 12f) + phase;
                Vector2 ray = a.ToRotationVector2();
                bool inward = tick == 9;
                Dust d = Dust.NewDustPerfect(center + ray * (inward ? 205f : tick == 6 ? 255f : 85f) * scale,
                    tick == 6 ? DustID.TintableDustLighted : DustID.BlueCrystalShard,
                    ray * (inward ? -3.8f : 2.8f) + ray.RotatedBy(MathHelper.PiOver2) * .7f,
                    90, MostimaClockwork.TimeColor(i % 3 == 0 ? MostimaVisuals.Violet : MostimaVisuals.Blue, gold), beatArrived ? 1.15f : .8f);
                d.noGravity = true; d.noLight = true;
                d.fadeIn = 0f;
                MostimaClockDustSystem.Track(d, center);
            }
        if (gold > .1f && (beatArrived || tick == 6) && Permit(center, true))
            for (int i = 0; i < 12; i++)
            {
                if (MostimaClockDustSystem.Count >= 256 || !Permit(center, true)) break;
                Vector2 ray = (i * MathHelper.TwoPi / 12f + phase).ToRotationVector2();
                Dust d = Dust.NewDustPerfect(center + ray * 275f * scale, DustID.TintableDustLighted,
                    ray * 4f, 60, MostimaClockwork.TimeColor(MostimaClockwork.Brass, gold), 1.15f);
                d.noGravity = true; d.noLight = true;
                MostimaClockDustSystem.Track(d, center);
            }
        if (gold > .1f && beatArrived)
            for (int i = 0; i < DecorationCount; i++)
            {
                Vector2 ray = (i * MathHelper.TwoPi / DecorationCount + phase).ToRotationVector2();
                Color amber = MostimaClockwork.TimeColor(MostimaClockwork.Brass, gold);
                Light(center, ray * 275f * scale, amber, ray.ToRotation(), new Vector2(1.2f, .22f), 18, 2.5f);
                Light(center, ray * 345f * scale, amber, ray.ToRotation() + MathHelper.PiOver2, new Vector2(.7f, .17f), 14, 3.2f);
            }
    }
    internal static void Draw(Vector2 center, float age, float opacity = 1f, float scale = 1f, float gold = 0f)
    {
        if (age < 12 || !SpreadVfxBudget.Visible(center, 540f)) return;
        float phase = age * .006f, pulse = (1f - Math.Max(0f, MostimaClockwork.BeatAge(age)) / MostimaClockwork.BeatTicks) * opacity;
        // Eighteen ornament stations, distinct from the twelve hour divisions.
        // Opposed tracks and alternating satellite markings suggest orbital machinery.
        for (int i = 0; i < DecorationCount; i++)
        {
            float angle = i * MathHelper.TwoPi / DecorationCount;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = (ring == 0 ? 302f : 376f) * scale;
                float start = angle + (ring == 0 ? phase : -phase);
                Vector2 previous = center + start.ToRotationVector2() * radius;
                for (int j = 1; j <= 8; j++)
                {
                    Vector2 next = center + (start + j * .025f).ToRotationVector2() * radius;
                    MostimaClockwork.Line(previous, next, OperatorFlightVfx.Add(MostimaClockwork.TimeColor(ring == 0 ? MostimaVisuals.Violet : MostimaClockwork.Brass, gold),
                        (.26f * opacity + pulse * .3f) * MathF.Sin(j / 9f * MathHelper.Pi)), 1.8f);
                    previous = next;
                }
            }
            Vector2 ray = (angle - phase).ToRotationVector2(), side = new(-ray.Y, ray.X);
            Vector2 p = center + ray * 338f * scale;
            Color tint = OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaClockwork.Brass, gold), .55f * opacity + pulse * .2f);
            MostimaClockwork.Line(p - ray * 12f, p + side * 7f, tint, 1.8f);
            MostimaClockwork.Line(p + side * 7f, p + ray * 12f, tint, 1.8f);
            MostimaClockwork.Line(p + ray * 12f, p - side * 7f, tint, 1.8f);
            MostimaClockwork.Line(p - side * 7f, p - ray * 12f, tint, 1.8f);
            MostimaClockwork.Line(p - side * 11f, p + side * 11f, tint * .7f, 1f);
            Vector2 satellite = center + ray * (i % 3 == 0 ? 394f : 385f) * scale;
            MostimaClockwork.Line(satellite - side * 4f, satellite + side * 4f, tint * .65f, 1.2f);
            MostimaClockwork.Line(satellite - ray * 3f, satellite + ray * 3f, tint * .65f, 1.2f);
        }
    }

    internal static void Hit(Vector2 center, float size, float gold = 0f)
    {
        if (!SpreadVfxBudget.Visible(center, 180f)) return;
        size = MathHelper.Clamp(size, 16f, 64f);
        int count = gold > .1f ? 18 : 12;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.07f, .07f);
            Vector2 ray = angle.ToRotationVector2();
            Color tint = MostimaClockwork.TimeColor(i % 3 == 0 ? MostimaVisuals.Violet : MostimaVisuals.Blue, gold);
            // Two optical points on each ray, kept apart instead of a solid white core.
            for (int j = 0; j < 2; j++)
                Light(center, ray * (size * .2f + j * 9f), tint, angle,
                    new Vector2(j == 0 ? .8f : .42f, .16f), 16 + j * 4, 2.8f + j * 1.7f);
            if (SpreadVfxBudget.Take(center, 180f))
                new MostimaHitShard(center, angle, size, i % 3, gold).Spawn();
            if (i % 2 == 0 && Permit(center, true) && MostimaClockDustSystem.Count < 256)
            {
                Dust d = Dust.NewDustPerfect(center + ray * size * .25f, DustID.BlueCrystalShard,
                    ray * Main.rand.NextFloat(2f, 4f), 100, tint, .75f);
                d.noGravity = true; d.noLight = true;
                MostimaClockDustSystem.Track(d, center);
            }
        }
    }
}

// Only owns the Dust instances emitted above. Token checks prevent touching a
// recycled vanilla dust slot. Dust updates remain in Terraria's dust system.
internal sealed class MostimaClockDustSystem : ModSystem
{
    private sealed class Entry { internal Dust Dust; internal Vector2 Center; internal int Age; }
    private static readonly List<Entry> Active = new();
    internal static int Count => Active.Count;
    internal static void Track(Dust dust, Vector2 center)
    {
        Entry entry = new() { Dust = dust, Center = center };
        dust.customData = entry; Active.Add(entry);
    }
    public override void PostUpdateDusts()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            Entry e = Active[i];
            if (!e.Dust.active || !ReferenceEquals(e.Dust.customData, e)) { Active.RemoveAt(i); continue; }
            e.Age++;
            if (e.Age >= 24 || Vector2.DistanceSquared(e.Dust.position, e.Center) > 390f * 390f)
            { e.Dust.active = false; Active.RemoveAt(i); }
            else if (e.Age > 14) { e.Dust.alpha = Math.Min(255, e.Dust.alpha + 18); e.Dust.scale *= .9f; }
        }
    }
    public override void OnWorldUnload() => Active.Clear();
    public override void Unload() => Active.Clear();
}

// A projected flight path crossing the portal: distant small point -> stretched
// foreground streak. Immutable world origin; movement never follows the caster.
public sealed class MostimaTimeTransit : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center, axis, side;
    private readonly float gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal MostimaTimeTransit(Vector2 center, float angle, float gold = 0f)
    {
        this.center = center; this.gold = gold; axis = angle.ToRotationVector2(); side = axis.RotatedBy(MathHelper.PiOver2);
        Lifetime = 30; Position = Point(0f); Rotation = angle;
    }
    private Vector2 Point(float t)
    {
        float distance = MathHelper.Lerp(-660f, 660f, t * t * (3f - 2f * t));
        return center + axis * distance + side * MathF.Sin(t * MathHelper.Pi) * 50f;
    }
    public override void Update()
    {
        Position = Point(LifetimeRatio);
        Color = MostimaClockwork.TimeColor(Color.Lerp(MostimaVisuals.Violet, MostimaClockwork.Silver, LifetimeRatio), gold)
            * (MathF.Sin(MathHelper.Pi * LifetimeRatio) * .65f);
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 100f)) return;
        Vector2 previous = Point(Math.Max(0f, LifetimeRatio - .018f));
        Vector2 trail = Position - previous;
        float length = MathHelper.Clamp(trail.Length() * 2.5f, 6f, 85f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, trail.ToRotation(),
            Texture.Size() * .5f, new Vector2(length, 3f + 7f * LifetimeRatio) / Texture.Size(), SpriteEffects.None, 0f);
    }
}

public sealed class MostimaRitualParticle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center;
    private readonly float angle;
    private readonly int kind;
    private readonly bool alternate;
    private readonly float scale, gold;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    internal MostimaRitualParticle(Vector2 center, float angle, int kind, bool alternate, float scale = 1f, float gold = 0f)
    {
        this.center = center; this.angle = angle; this.kind = kind; this.alternate = alternate; this.scale = scale; this.gold = gold;
        Lifetime = kind == 0 ? 26 : kind == 1 ? 22 : 30;
        Position = Point(0);
    }
    private Vector2 Point(float t)
    {
        float a = angle + (alternate ? 1f : -1f) * (kind == 1 ? .85f : .22f) * t;
        float radius = kind == 0 ? 80f + 275f * (1f - MathF.Pow(1f - t, 3f))
            : kind == 1 ? 280f - 215f * t : 295f + 65f * MathF.Sin(t * MathHelper.PiOver2);
        return center + a.ToRotationVector2() * radius * scale;
    }
    public override void Update()
    {
        Position = Point(LifetimeRatio); Rotation = angle + LifetimeRatio * (alternate ? 1 : -1);
        float fade = MathHelper.Clamp(Time / 3f, 0f, 1f) * MathF.Pow(1f - LifetimeRatio, .9f);
        Color = MostimaClockwork.TimeColor(kind == 2 ? MostimaClockwork.Brass : alternate ? MostimaVisuals.Blue : MostimaVisuals.Violet, gold) * fade;
    }
    public override void Draw()
    {
        if (!SpreadVfxBudget.Visible(Position, 80f)) return;
        if (kind == 0)
            SpreadVfxDetail.Facet(Position, 7f, 23f * (1f - LifetimeRatio * .6f), Rotation, Color, 1f);
        else if (kind == 1)
        {
            Vector2 previous = Position;
            for (int i = 1; i <= 9; i++)
            {
                Vector2 next = Point(MathF.Max(0f, LifetimeRatio - i * .014f));
                MostimaClockwork.Line(previous, next, OperatorFlightVfx.Add(Color, 1f - i / 10f), 3f - i * .25f);
                previous = next;
            }
        }
        else
        {
            Vector2 ray = Rotation.ToRotationVector2(), side = new(-ray.Y, ray.X);
            MostimaClockwork.Line(Position - ray * 9f, Position + ray * 9f, Color, 2f);
            MostimaClockwork.Line(Position - side * 5f, Position + side * 5f, Color, 1.5f);
            MostimaClockwork.Line(Position + ray * 9f, Position + ray * 3f + side * 6f, Color, 1.5f);
        }
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, OperatorFlightVfx.Add(Color, .5f), 0f,
            Texture.Size() * .5f, new Vector2(kind == 2 ? 22f : 15f) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
