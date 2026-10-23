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
    internal const float Radius = 24, Gravity = .08f, InitialSpeed = 1.2f;
    internal static int OriginalCount(int attack) => attack == 19 ? 18 : attack is 20 or 21 ? 12 : 0;
    // Old phase-two Shoot added 50% companion balls: 27/18/18 in total. New totals are 41/27/27.
    internal static int Count(int attack) => (int)MathF.Round(OriginalCount(attack) * 1.5f * 1.5f, MidpointRounding.AwayFromZero);
    internal static EvolutionRicochetNote Note(int attack, int index)
    {
        int original = OriginalCount(attack);
        if (index < 0 || index >= Count(attack)) throw new ArgumentOutOfRangeException(nameof(index));
        int source = index % original, extra = index / original;
        var note = BaseNote(attack, source);
        if (extra > 0) note = note with { Tick = note.Tick + extra * 8,
            Offset = note.Offset + (note.Edge == 0 ? new Vector2(0, (source % 2 == 0 ? -1 : 1) * extra * 42) : new Vector2(extra * 42, 0)) };
        return note with { Velocity = note.Velocity * InitialSpeed };
    }
    private static EvolutionRicochetNote BaseNote(int attack, int index)
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
    private static float BoundaryTime(float y, float speed, float top, float bottom, bool initial)
    {
        if (initial && (y <= top + .001f && speed < 0 || y >= bottom - .001f && speed > 0)) return 0;
        return Math.Min(PlaneTime(y, speed, top), PlaneTime(y, speed, bottom));
    }
    private static float PlaneTime(float y, float speed, float wall)
    {
        float square = speed * speed + 2 * Gravity * (wall - y);
        if (square < 0) return float.PositiveInfinity;
        float root = MathF.Sqrt(square), early = (-speed - root) / Gravity, late = (-speed + root) / Gravity;
        return early > .001f ? early : late > .001f ? late : float.PositiveInfinity;
    }
    private readonly record struct Flight(Vector2 Start, Vector2 Velocity, Vector2 Bend, Vector2 Reflected, Vector2 End, float First, float Last);
    private static Flight Plan(Vector2 origin, Vector2 velocity, float top, float bottom)
    {
        float upper = top + Radius, lower = bottom - Radius;
        Vector2 start = new(origin.X, Math.Clamp(origin.Y, upper, lower)), v = Velocity(velocity);
        float first = BoundaryTime(start.Y, v.Y, upper, lower, true);
        Vector2 bend = start + v * first + Vector2.UnitY * (Gravity * first * first * .5f);
        bend.Y = Math.Abs(bend.Y - upper) < Math.Abs(bend.Y - lower) ? upper : lower;
        Vector2 reflected = new(v.X, -(v.Y + Gravity * first));
        float second = BoundaryTime(bend.Y, reflected.Y, upper, lower, false);
        Vector2 end = bend + reflected * second + Vector2.UnitY * (Gravity * second * second * .5f);
        end.Y = Math.Abs(end.Y - upper) < Math.Abs(end.Y - lower) ? upper : lower;
        return new(start, v, bend, reflected, end, first, first + second);
    }
    internal static float FirstHit(Vector2 origin, Vector2 velocity, float top, float bottom) => Plan(origin, velocity, top, bottom).First;
    internal static float EndTime(Vector2 origin, Vector2 velocity, float top, float bottom) => Plan(origin, velocity, top, bottom).Last;
    internal static int Lifetime(Vector2 origin, Vector2 velocity, int delay, float top, float bottom) =>
        delay + (int)Math.Ceiling(EndTime(origin, velocity, top, bottom)) + Fade;
    internal static EvolutionRicochetSample Sample(Vector2 origin, Vector2 velocity, float elapsed, float top, float bottom)
    {
        Flight path = Plan(origin, velocity, top, bottom);
        float time = Math.Clamp(elapsed, 0, path.Last);
        bool bounced = elapsed >= path.First, finished = elapsed >= path.Last;
        float local = bounced ? time - path.First : time;
        Vector2 initial = bounced ? path.Reflected : path.Velocity;
        Vector2 center = finished ? path.End : (bounced ? path.Bend : path.Start) + initial * local + Vector2.UnitY * (Gravity * local * local * .5f);
        return new(center, initial + Vector2.UnitY * (Gravity * local), bounced, finished);
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
            EvolutionVisuals.Glow(state.Center, tint * (.4f * warning), new Vector2(50));
            EvolutionVisuals.Line(state.Center, state.Center + state.Direction.SafeNormalize(Vector2.UnitY) * 100,
                tint * (.5f * warning), 1.3f, false);
            return;
        }
        if (!state.Finished)
            for (int past = 1; past <= 4; past++)
            {
                Vector2 point = EvolutionRicochetRules.Sample(ArsenalOrigin, Projectile.velocity, elapsed - past * 1.5f,
                    boss.CorridorTop, boss.CorridorBottom).Center;
                EvolutionVisuals.Glow(point, tint * ((5 - past) * .06f * opacity), new Vector2((19 - past * 2) * 2));
            }
        EvolutionVisuals.Glow(state.Center, tint * (.8f * opacity), new Vector2(68));
        EvolutionVisuals.Glow(state.Center, EvolutionVisuals.Core * opacity, new Vector2(26));
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        Main.spriteBatch.Draw(ring, state.Center - Main.screenPosition, null, (tint with { A = 0 }) * (.65f * opacity),
            Age * .07f, ring.Size() * .5f, 50f / ring.Width, SpriteEffects.None, 0);
    }
}
