using ArknightsMod.Common.Particle.T;
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
    private static readonly ParticlePool<T_Lighting_Bolts_Particle> Pool = new(256, () => new());
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
        T_Lighting_Bolts_Particle p = Pool.RequestParticle();
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
    // The real visual frame is independent of the skill-scaled hand/attack clock.
    // Sunset's radial/tangential layering and Revelation's counter-spirals are
    // rebuilt locally, with a steady birth rate rather than complete ring bursts.
    internal static void Tick(Projectile clock, int frame, float scale, float gold)
    {
        Vector2 center = clock.Center;
        if (!SpreadVfxBudget.Visible(center, 580f * scale)) return;
        // Keep radial outflow/inflow. The four coherent spiral lanes were removed:
        // their overlapping tails formed large off-center rotating arcs.
        for (int mode = 0; mode < 2; mode++)
        for (int pair = 0; pair < 2; pair++)
        {
            float angle = frame * 2.39996323f + mode * .83f + pair * MathHelper.Pi;
            float handedness = pair == 0 ? 1f : -1f;
            if (SpreadVfxBudget.Take(center, 580f * scale))
                new T_Chronal_Stream_Particle(clock, mode, angle, handedness, scale, gold).Spawn();

            if (Permit(center, true) && MostimaClockDustSystem.Count < 256)
            {
                Vector2 point = MostimaTemporalFlow.Point(mode, angle, handedness, 0f, scale);
                Vector2 velocity = (MostimaTemporalFlow.Point(mode, angle, handedness, .04f, scale) - point);
                Color tint = MostimaTemporalFlow.Tint(mode, gold);
                Dust d = Dust.NewDustPerfect(center + point, mode == 1 ? DustID.BlueCrystalShard : DustID.TintableDustLighted,
                    velocity, 90, tint, .85f);
                d.noGravity = true; d.noLight = true;
                MostimaClockDustSystem.TrackFlow(d, clock, mode, angle, handedness, scale);
            }
        }
        // Sparse short optical glints share a tangent with the flowing particles.
        // These are vanilla PrettySparkles, not registrations in ParticleManager.
        for (int i = 0; i < 2; i++)
        {
            int mode = (frame + i) % 3;
            float angle = frame * 2.39996323f + i * MathHelper.Pi;
            float u = .2f + .6f * ((frame * .6180339f + i * .5f) % 1f);
            Vector2 point = MostimaTemporalFlow.Point(mode, angle, 1f, u, scale);
            Vector2 tangent = MostimaTemporalFlow.Point(mode, angle, 1f, u + .012f, scale) - point;
            if (!Permit(center, false)) break;
            T_Lighting_Bolts_Particle p = Pool.RequestParticle();
            p.LocalPosition = center + point; p.Velocity = tangent;
            p.Rotation = tangent.ToRotation();
            p.ColorTint = MostimaTemporalFlow.Tint(mode, gold) * .7f;
            p.Scale = new Vector2(.52f, .13f); p.TimeToLive = 9;
            p.FadeInNormalizedTime = .08f; p.FadeOutNormalizedTime = .65f;
            p.FadeInEnd = 8; p.FadeOutStart = 30; p.FadeOutEnd = 60;
            p.AdditiveAmount = .7f;
            Main.ParticleSystem_World_OverPlayers.Add(p);
        }
    }

    internal static void Draw(Vector2 center, float time, float opacity = 1f, float scale = 1f, float gold = 0f)
    {
        if (!SpreadVfxBudget.Visible(center, 580f * scale)) return;
        // Quiet paired orbital rails: constant brightness and continuous rotation.
        // No diamonds, star stations, dark smears, or beat-driven expanding circles.
        for (int track = 0; track < 2; track++)
        for (int arm = 0; arm < 3; arm++)
        {
            float radius = (track == 0 ? 294f : 310f) * scale;
            float phase = time * (track == 0 ? .004f : -.003f) + arm * MathHelper.TwoPi / 3f;
            Color tint = OperatorFlightVfx.Add(MostimaClockwork.TimeColor(track == 0 ? MostimaVisuals.Blue : MostimaClockwork.Brass, gold), opacity * .24f);
            for (int j = 0; j < 30; j++)
            {
                float a = phase + j * .047f;
                MostimaClockwork.Line(center + a.ToRotationVector2() * radius,
                    center + (a + .047f).ToRotationVector2() * radius,
                    tint * MathF.Sin((j + .5f) / 30f * MathHelper.Pi), scale);
            }
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
            Color tint = MostimaClockwork.TimeColor(i % 3 == 0 ? new Color(190, 243, 255) : new Color(42, 174, 255), gold);
            // Short blue-white fragments surround the compact impact core.
            for (int j = 0; j < 2; j++)
                Light(center, ray * (size * .14f + j * 5f), tint, angle,
                    new Vector2(j == 0 ? .5f : .28f, .12f), 10 + j * 4, 1.8f + j * 1.2f);
            if (SpreadVfxBudget.Take(center, 180f))
                new T_Clock_HitShard_Particle(center, angle, size, i % 3, gold).Spawn();
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
    private sealed class Entry { internal Dust Dust; internal Vector2 Center; internal int Age; internal Projectile Clock; internal int Identity, Mode; internal float Angle, Hand, Scale; }
    private static readonly List<Entry> Active = new();
    internal static int Count => Active.Count;
    internal static void Track(Dust dust, Vector2 center)
    {
        Entry entry = new() { Dust = dust, Center = center };
        dust.customData = entry; Active.Add(entry);
    }
    internal static void TrackFlow(Dust dust, Projectile clock, int mode, float angle, float hand, float scale)
    {
        Entry entry = new() { Dust = dust, Center = clock.Center, Clock = clock, Identity = clock.identity,
            Mode = mode, Angle = angle, Hand = hand, Scale = scale };
        dust.customData = entry; Active.Add(entry);
    }
    public override void PostUpdateDusts()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            Entry e = Active[i];
            if (!e.Dust.active || !ReferenceEquals(e.Dust.customData, e)) { Active.RemoveAt(i); continue; }
            e.Age++;
            if (e.Clock != null)
            {
                float visibility = e.Clock.active && e.Clock.identity == e.Identity
                    ? MathHelper.Clamp(1f - e.Clock.ai[1] / MostimaClockwork.FadeTicks, 0f, 1f) : 0f;
                if (e.Age >= 24 || visibility <= 0f)
                { e.Dust.active = false; Active.RemoveAt(i); continue; }
                float t = e.Age / 24f;
                e.Dust.position = e.Center + MostimaTemporalFlow.Point(e.Mode, e.Angle, e.Hand, t, e.Scale);
                e.Dust.velocity = Vector2.Zero;
                // Keep physical Dust scale above vanilla's early-death threshold;
                // opacity supplies the birth/death envelope instead.
                e.Dust.scale = .65f + .2f * MathF.Sin(MathHelper.Pi * t);
                e.Dust.alpha = 255 - (int)(165f * MathF.Sin(MathHelper.Pi * t) * visibility);
                continue;
            }
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
