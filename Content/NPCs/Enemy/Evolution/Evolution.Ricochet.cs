using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal readonly record struct EvolutionRicochetNote(int Tick, Vector2 Offset, Vector2 Velocity, int Edge = 0);
internal readonly record struct EvolutionRicochetSample(Vector2 Center, Vector2 Direction, bool Bounced, bool Finished);

internal static class EvolutionRicochetRules
{
    internal const int Duration = 282, Warning = 22, Fade = 16;
    internal const float Radius = 12;
    internal static int Count(int attack) => attack == 19 ? 18 : attack is 20 or 21 ? 12 : 0;
    internal static EvolutionRicochetNote Note(int attack, int index)
    {
        int wave = index / 6, key = index % 6;
        if (attack == 19)
        {
            int side = key < 3 ? -1 : 1, row = key % 3;
            return new(12 + wave * 46, new Vector2(side * 520, (row - 1) * 110),
                new Vector2(-side * (6 + row * 1.2f + wave * .5f), (row + wave) % 2 == 0 ? 12 + row : -12 - row));
        }
        if (attack == 20)
            return new(12 + wave * 74, new Vector2(EvolutionRules.FanSlot(key, 6) * 144, 0),
                new Vector2(key % 2 == 0 ? 4 : -4, wave == 0 ? 13 : -13), wave == 0 ? -1 : 1);
        float angle = (wave == 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2) + EvolutionRules.FanSlot(key, 6) * .17f;
        return new(14 + wave * 74, new Vector2(wave == 0 ? -450 : 450, 0), angle.ToRotationVector2() * 18);
    }
    internal static Vector2 Velocity(Vector2 velocity) => Math.Abs(velocity.Y) < 1 ? new(velocity.X, velocity.Y < 0 ? -12 : 12) : velocity;
    internal static EvolutionRayPath Path(Vector2 origin, Vector2 velocity, float top, float bottom) =>
        EvolutionRefractionRules.Trace(new Vector2(origin.X, Math.Clamp(origin.Y, top + Radius, bottom - Radius)),
            Velocity(velocity), 0, top + Radius, bottom - Radius);
    internal static float FirstHit(Vector2 origin, Vector2 velocity, float top, float bottom)
    {
        EvolutionRayPath path = Path(origin, velocity, top, bottom);
        return Vector2.Distance(path.Start, path.Bend) / Velocity(velocity).Length();
    }
    internal static float EndTime(Vector2 origin, Vector2 velocity, float top, float bottom) =>
        FirstHit(origin, velocity, top, bottom) + (bottom - top - Radius * 2) / Math.Abs(Velocity(velocity).Y);
    internal static int Lifetime(Vector2 origin, Vector2 velocity, int delay, float top, float bottom) =>
        delay + (int)Math.Ceiling(EndTime(origin, velocity, top, bottom)) + Fade;
    internal static EvolutionRicochetSample Sample(Vector2 origin, Vector2 velocity, float elapsed, float top, float bottom)
    {
        Vector2 v = Velocity(velocity);
        EvolutionRayPath path = Path(origin, v, top, bottom);
        float first = Vector2.Distance(path.Start, path.Bend) / v.Length();
        float end = first + (bottom - top - Radius * 2) / Math.Abs(v.Y);
        bool bounced = elapsed >= first;
        Vector2 direction = bounced ? new(v.X, -v.Y) : v;
        Vector2 center = elapsed >= end ? path.End : bounced ? path.Bend + direction * (elapsed - first) : path.Start + v * Math.Max(0, elapsed);
        return new(center, direction, bounced, elapsed >= end);
    }
}

public sealed partial class Evolution
{
    private void DoRicochetPattern()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Attack == 22)
        {
            if (Timer is not (12 or 68 or 124)) return;
            int wave = (Timer - 12) / 56;
            for (int side = -1; side <= 1; side += 2)
            {
                bool down = (wave + (side > 0 ? 1 : 0)) % 2 == 0;
                Vector2 source = new(PatternOrigin.X + side * 470, down ? CorridorTop + 42 : CorridorBottom - 42);
                Shoot(EvolutionShot.Beam, source, new Vector2(-side * .8f, down ? 1 : -1), 1000,
                    40, 156, quietLaunch: true, beamStyle: EvolutionBeamStyle.Refracted);
            }
            return;
        }
        for (int index = 0; index < EvolutionRicochetRules.Count(Attack); index++)
        {
            EvolutionRicochetNote note = EvolutionRicochetRules.Note(Attack, index);
            if (Timer != note.Tick) continue;
            Vector2 source = new(PatternOrigin.X + note.Offset.X, note.Edge < 0 ? CorridorTop + 16 :
                note.Edge > 0 ? CorridorBottom - 16 : Math.Clamp(Arena.Y + note.Offset.Y, CorridorTop + 60, CorridorBottom - 60));
            Shoot(EvolutionShot.Ricochet, source, note.Velocity, delay: EvolutionRicochetRules.Warning, quietLaunch: true);
        }
    }
}

public sealed partial class EvolutionHazard
{
    private int ricochetContactCues;
    internal EvolutionRicochetSample RicochetState => Parent is Evolution boss ?
        EvolutionRicochetRules.Sample(ArsenalOrigin, Projectile.velocity, Age - Delay, boss.CorridorTop, boss.CorridorBottom) :
        new(Projectile.Center, Projectile.velocity, false, true);
    private void UpdateRicochet(Evolution boss)
    {
        EvolutionRicochetSample state = RicochetState;
        Projectile.Center = state.Center;
        Projectile.rotation = state.Direction.ToRotation();
        if (Main.dedServ || Age < Delay) return;
        int elapsed = Age - Delay;
        int first = (int)Math.Ceiling(EvolutionRicochetRules.FirstHit(ArsenalOrigin, Projectile.velocity, boss.CorridorTop, boss.CorridorBottom));
        int end = (int)Math.Ceiling(EvolutionRicochetRules.EndTime(ArsenalOrigin, Projectile.velocity, boss.CorridorTop, boss.CorridorBottom));
        int cue = elapsed == first ? 1 : elapsed == end ? 2 : 0;
        if (cue != 0 && (ricochetContactCues & cue) == 0)
        {
            ricochetContactCues |= cue;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = .18f, Pitch = .2f, MaxInstances = 2 }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Red,
                    Main.rand.NextVector2Circular(2, 2), 0, EvolutionVisuals.Core, .8f);
                dust.noGravity = true;
            }
        }
        if (!state.Finished) Lighting.AddLight(Projectile.Center, EvolutionVisuals.Blood.ToVector3() * .25f);
    }
    private void DrawRicochet()
    {
        if (Parent is not Evolution boss) return;
        EvolutionRicochetSample state = RicochetState;
        int elapsed = Age - Delay;
        float end = EvolutionRicochetRules.EndTime(ArsenalOrigin, Projectile.velocity, boss.CorridorTop, boss.CorridorBottom);
        float opacity = Math.Clamp((end + EvolutionRicochetRules.Fade - elapsed) / EvolutionRicochetRules.Fade, 0, 1);
        Color tint = state.Bounced ? EvolutionVisuals.Blood : EvolutionVisuals.Core;
        if (elapsed < 0)
        {
            float warning = Math.Clamp(Age / 8f, 0, 1);
            EvolutionVisuals.Glow(state.Center, tint * (.4f * warning), new Vector2(25));
            EvolutionVisuals.Line(state.Center, state.Center + state.Direction.SafeNormalize(Vector2.UnitY) * 100,
                tint * (.5f * warning), 1.3f, false);
            return;
        }
        if (!state.Finished)
            for (int past = 1; past <= 4; past++)
            {
                Vector2 point = EvolutionRicochetRules.Sample(ArsenalOrigin, Projectile.velocity, elapsed - past * 1.5f,
                    boss.CorridorTop, boss.CorridorBottom).Center;
                EvolutionVisuals.Glow(point, tint * ((5 - past) * .06f * opacity), new Vector2(19 - past * 2));
            }
        EvolutionVisuals.Glow(state.Center, tint * (.8f * opacity), new Vector2(34));
        EvolutionVisuals.Glow(state.Center, EvolutionVisuals.Core * opacity, new Vector2(13));
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        Main.spriteBatch.Draw(ring, state.Center - Main.screenPosition, null, (tint with { A = 0 }) * (.65f * opacity),
            Age * .07f, ring.Size() * .5f, 25f / ring.Width, SpriteEffects.None, 0);
    }
}
