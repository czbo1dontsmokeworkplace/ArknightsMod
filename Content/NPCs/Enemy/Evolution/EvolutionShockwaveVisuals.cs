using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Shared presentation for every outward impact. No entities, collision or gameplay RNG.
internal static class EvolutionShockwaveVisuals
{
    // The original full circle is strictly a draw layer; only ShockwaveRules.Collides can hurt.
    internal const float FullCircleReach = 1280;
    internal const int FullCircleDuration = 72; // Original 36 ticks, preserving the doubled reaction time.
    internal static void DrawPressureFront(SpriteBatch batch, Effect effect, Texture2D noise, Vector2 at,
        int age, float axis, Color color, Color core)
    {
        if (age < 0 || age >= EvolutionShockwaveRules.Duration) return;
        // Both layers use the actual projectile age, so late snapshots never start a second clock.
        if (age < FullCircleDuration)
            DrawFront(batch, effect, noise, at, FullCircleReach, age, FullCircleDuration, 1.035f, color, core,
                animationRate: .5f);
        DrawFront(batch, effect, noise, at, EvolutionShockwaveRules.Reach, age, EvolutionShockwaveRules.Duration,
            1.035f, color, core, axis, EvolutionShockwaveRules.GapCount, EvolutionShockwaveRules.GapHalfWidth, .5f);
    }
    internal static void DrawPressureDebris(SpriteBatch batch, Texture2D bloom, Texture2D fleck, Texture2D ring,
        Vector2 at, int age, float axis, Color color, Color core, bool reduced, bool fallback)
    {
        if (age < 0 || age >= EvolutionShockwaveRules.Duration) return;
        if (age < FullCircleDuration)
            DrawDebris(batch, bloom, fleck, ring, at, FullCircleReach, age, FullCircleDuration, 1.035f,
                color, core, reduced, fallback, animationRate: .5f);
        DrawDebris(batch, bloom, fleck, ring, at, EvolutionShockwaveRules.Reach, age, EvolutionShockwaveRules.Duration,
            1.035f, color, core, reduced, fallback, axis, EvolutionShockwaveRules.GapCount, EvolutionShockwaveRules.GapHalfWidth, .5f);
    }
    internal static float Progress(int age, int duration) => Math.Clamp(age / (float)Math.Max(1, duration), 0, 1);
    internal static float Radius(float radius, float u) => 18 + (radius - 18) * (1 - MathF.Pow(1 - u, 2.6f));
    internal static float Opacity(float u) => (1 - Smooth(.52f, 1, u)) * (1.05f - u * .45f);
    internal static float Width(float radius, float u) => Math.Max(16, radius * (.12f - u * .03f));
    private static float Smooth(float a, float b, float u) { u = Math.Clamp((u - a) / (b - a), 0, 1); return u * u * (3 - 2 * u); }
    private static float Hash(int i) { float n = MathF.Sin(i * 127.1f + 311.7f) * 43758.5453f; return n - MathF.Floor(n); }

    // Caller supplies an immediate SpriteBatch, the material and an existing grayscale texture.
    internal static void DrawFront(SpriteBatch batch, Effect effect, Texture2D noise, Vector2 at,
        float radius, int age, int duration, float power, Color color, Color core,
        float gapAngle = 0, int gapCount = 0, float gapHalfWidth = 0, float animationRate = 1)
    {
        effect.CurrentTechnique = effect.Techniques["PressureFront"];
        float u = Progress(age, duration), extent = Radius(radius, u) + Width(radius, u) * 4 + 48;
        effect.Parameters["uAge"].SetValue(age * animationRate);
        effect.Parameters["uLife"].SetValue(u);
        effect.Parameters["uRadius"].SetValue(Radius(radius, u));
        effect.Parameters["uExtent"].SetValue(extent);
        effect.Parameters["uWidth"].SetValue(Width(radius, u));
        effect.Parameters["uPower"].SetValue(power);
        effect.Parameters["uColor"].SetValue(color.ToVector3());
        effect.Parameters["uCore"].SetValue(core.ToVector3());
        SetGaps(effect, gapAngle, gapCount, gapHalfWidth);
        effect.CurrentTechnique.Passes[0].Apply();
        batch.Draw(noise, at, null, Color.White, 0, new Vector2(noise.Width, noise.Height) * .5f,
            new Vector2(extent * 2 / noise.Width, extent * 2 / noise.Height), SpriteEffects.None, 0);
    }

    internal static void DrawDebris(SpriteBatch batch, Texture2D bloom, Texture2D fleck, Texture2D ring,
        Vector2 at, float radius, int age, int duration, float power, Color color, Color core, bool reduced, bool fallback,
        float gapAngle = 0, int gapCount = 0, float gapHalfWidth = 0, float animationRate = 1)
    {
        float u = Progress(age, duration), front = Radius(radius, u), fade = Opacity(u);
        Vector2 bloomOrigin = new(bloom.Width * .5f, bloom.Height * .5f);
        void Glow(Vector2 p, Color c, Vector2 size, float angle = 0) =>
            batch.Draw(bloom, p, null, c with { A = 0 }, angle, bloomOrigin,
                new Vector2(size.X / bloom.Width, size.Y / bloom.Height), SpriteEffects.None, 0);
        bool Safe(float angle) => gapCount > 0 && Math.Abs(MathF.IEEERemainder(angle - gapAngle, MathHelper.TwoPi / gapCount)) <= gapHalfWidth + .02f;
        if ((fallback || reduced) && gapCount > 0)
        {
            // A segmented pressure band preserves the real openings even without shaders.
            const int steps = 240;
            float da = MathHelper.TwoPi / steps;
            for (int i = 0; i < steps; i++)
            {
                float a = (i + .5f) * da;
                if (Safe(a) || Safe(a - da * .5f) || Safe(a + da * .5f)) continue;
                Vector2 p = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * front;
                float width = Width(radius, u) * (reduced ? .28f : .45f);
                Glow(p, color * (fade * (reduced ? .48f : .75f)), new Vector2(width * 2.2f, front * da * 2.4f), a);
                batch.Draw(fleck, p, new Rectangle(0, 0, 1, 1), (core with { A = 0 }) * (fade * .72f),
                    a, new Vector2(.5f), new Vector2(width * .3f, front * da + 1), SpriteEffects.None, 0);
            }
        }
        else if (fallback || reduced)
        {
            for (int layer = 0; layer < (reduced ? 1 : 3); layer++)
            {
                float r = Math.Max(8, front - layer * Width(radius, u) * .6f);
                batch.Draw(ring, at, null, ((layer == 0 ? core : color) with { A = 0 }) * (fade * (reduced ? .32f : .65f) / (layer + 1)),
                    0, new Vector2(ring.Width, ring.Height) * .5f, new Vector2(r * 2 / ring.Width, r * 2 / ring.Height), SpriteEffects.None, 0);
            }
        }
        float ignition = MathF.Pow(Math.Max(0, 1 - age * animationRate / 7f), 2);
        Glow(at, core * (ignition * (reduced ? .25f : 1.1f)), new Vector2(gapCount > 0 ? 95 : 80 + radius * .26f));
        if (reduced) return;
        int count = radius >= 600 ? 80 : 32;
        for (int i = 0; i < count; i++)
        {
            float seed = Hash(i + 19), angle = (i + seed * .75f) * MathHelper.TwoPi / count;
            if (Safe(angle)) continue;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float travel = Radius(radius, Math.Clamp(u * (.66f + Hash(i + 43) * .56f), 0, 1));
            Vector2 p = at + direction * travel;
            float particleFade = fade * (.5f + seed * .5f) * Smooth(0, .055f, u);
            float size = (3 + seed * 7) * Math.Clamp(radius / 700, .55f, 1.5f);
            Glow(p, color * (particleFade * .65f * power), new Vector2(size * 7, size * 3), angle);
            batch.Draw(fleck, p, null, (core with { A = 0 }) * (particleFade * .9f),
                angle + seed * 2 + u, new Vector2(fleck.Width, fleck.Height) * .5f,
                new Vector2(size * 1.4f / fleck.Width, size * .9f / fleck.Height), SpriteEffects.None, 0);
            // Tapering streaks travel with the front; the center clears instead of filling with fog.
            float length = (25 + seed * 100) * (1 - u) * Math.Clamp(radius / 760, .45f, 1.5f);
            Glow(p - direction * length * .5f, color * (particleFade * .45f), new Vector2(length, 2 + seed * 3), angle);
        }
    }

    internal static void SetGaps(Effect effect, float axis, int count, float halfWidth)
    {
        effect.Parameters["uGapAngle"].SetValue(axis);
        effect.Parameters["uGapCount"].SetValue((float)count);
        effect.Parameters["uGapHalfWidth"].SetValue(halfWidth);
    }
    internal static float WarningOpacity(int age, int delay, int fire) =>
        Smooth(0, 3 * delay / 30f, age) * (1 - Smooth(delay, Math.Max(delay + 1, fire), age));

    internal static void DrawWarningMaterial(SpriteBatch batch, Effect effect, Texture2D noise, Vector2 at,
        float reach, float axis, int age, int delay, int fire, Color color, Color core)
    {
        effect.CurrentTechnique = effect.Techniques["PressureWarning"];
        effect.Parameters["uExtent"].SetValue(reach + 48);
        effect.Parameters["uAge"].SetValue(age * 30f / Math.Max(1, delay));
        effect.Parameters["uWarningProgress"].SetValue(Math.Clamp(age / (float)Math.Max(1, delay), 0, 1));
        effect.Parameters["uWarningOpacity"].SetValue(WarningOpacity(age, delay, fire));
        effect.Parameters["uColor"].SetValue(color.ToVector3());
        effect.Parameters["uCore"].SetValue(core.ToVector3());
        SetGaps(effect, axis, EvolutionShockwaveRules.GapCount, EvolutionShockwaveRules.GapHalfWidth);
        effect.CurrentTechnique.Passes[0].Apply();
        batch.Draw(noise, at, null, Color.White, 0, new Vector2(noise.Width, noise.Height) * .5f,
            new Vector2((reach + 48) * 2 / noise.Width, (reach + 48) * 2 / noise.Height), SpriteEffects.None, 0);
    }

    internal static void DrawWarningGuides(SpriteBatch batch, Texture2D pixel, Texture2D bloom, Vector2 at,
        float reach, float axis, int age, int delay, int fire, Color color, Color core, bool reduced)
    {
        float alpha = WarningOpacity(age, delay, fire), progress = Math.Clamp(age / (float)Math.Max(1, delay), 0, 1);
        if (alpha <= 0) return;
        Vector2 Dir(float a) => new(MathF.Cos(a), MathF.Sin(a));
        void Line(Vector2 start, Vector2 end, Color tint, float width)
        {
            Vector2 d = end - start;
            batch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), tint, MathF.Atan2(d.Y, d.X), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
        }
        void Glow(Vector2 p, Color tint, Vector2 size, float angle) =>
            batch.Draw(bloom, p, null, tint with { A = 0 }, angle, new Vector2(bloom.Width, bloom.Height) * .5f,
                new Vector2(size.X / bloom.Width, size.Y / bloom.Height), SpriteEffects.None, 0);
        for (int i = 0; i < EvolutionShockwaveRules.GapCount; i++)
        {
            float a = axis + i * MathHelper.TwoPi / EvolutionShockwaveRules.GapCount;
            Vector2 direction = Dir(a), normal = Dir(a + MathHelper.PiOver2);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 edge = Dir(a + side * EvolutionShockwaveRules.GapHalfWidth);
                Line(at + edge * 70, at + edge * reach, Color.Black * (alpha * .55f), 6);
                Line(at + edge * 70, at + edge * reach, core * (alpha * .9f), 2.4f);
                if (!reduced) Glow(at + edge * (reach * .5f + 35), color * (alpha * .32f), new Vector2(reach - 70, 15), MathF.Atan2(edge.Y, edge.X));
            }
            // Arrow pairs and moving light beads identify these wedges as exits, not aimed lasers.
            for (int marker = 0; marker < (reduced ? 2 : 4); marker++)
            {
                float r = 180 + marker * 270 + progress * 55;
                Vector2 tip = at + direction * r;
                float size = 9 + marker * 3;
                Line(tip - direction * size - normal * size * .7f, tip, core * (alpha * .75f), 2.5f);
                Line(tip - direction * size + normal * size * .7f, tip, core * (alpha * .75f), 2.5f);
                if (!reduced) Glow(tip, color * (alpha * .42f), new Vector2(24 + marker * 4, 14), a);
            }
            // This compact countdown converges on the core, while the safe corridor stays fixed.
            Vector2 bead = at + direction * (150 - progress * 72);
            Glow(bead, core * (alpha * .9f), new Vector2(20 + progress * 10), a);
        }
    }
}
