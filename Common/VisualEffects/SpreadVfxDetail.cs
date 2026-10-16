using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Common.VisualEffects;

internal static class SpreadVfxBudget
{
    private static ulong frame;
    private static int emitted;
    internal static bool Visible(Vector2 center, float margin = 100f)
    {
        if (Main.dedServ || Main.gameMenu || !OperatorFlightVfx.Finite(center)) return false;
        float zoom = Math.Max(.25f, Main.GameViewMatrix.Zoom.X);
        Vector2 viewCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f;
        Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) * (.5f / zoom) + new Vector2(margin);
        return Math.Abs(center.X - viewCenter.X) <= half.X && Math.Abs(center.Y - viewCenter.Y) <= half.Y;
    }
    internal static bool Take(Vector2 center, float margin = 100f)
    {
        if (!Visible(center, margin) || ParticleManager.activeParticles.Count >= 1300) return false;
        if (frame != Main.GameUpdateCount) { frame = Main.GameUpdateCount; emitted = 0; }
        return emitted++ < 100;
    }
}

internal static class SpreadVfxDetail
{
    internal static void Disc(Vector2 center, float radius, Color tint)
    {
        radius = MathHelper.Clamp(radius, 1f, 24f);
        for (int y = -(int)radius; y <= (int)radius; y++)
        {
            float halfWidth = MathF.Sqrt(Math.Max(0f, radius * radius - y * y));
            Segment(center + new Vector2(-halfWidth, y), center + new Vector2(halfWidth, y), tint, 1.1f);
        }
    }
    internal static void Ellipse(Vector2 center, Vector2 aim, Vector2 radii, float start, float sweep, Color tint, float width)
    {
        radii = Vector2.Clamp(radii, Vector2.One, new Vector2(80f));
        Vector2 Point(float angle) => SpreadChargeVfx.AimPoint(center, aim, MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y);
        Vector2 previous = Point(start);
        for (int i = 1; i <= 40; i++)
        {
            Vector2 next = Point(start + sweep * i / 40f);
            Segment(previous, next, tint * MathF.Sin(MathHelper.Pi * (i - .5f) / 40f), width);
            previous = next;
        }
    }
    internal static void Fragment(Vector2 center, Vector2 velocity, Color tint, float size, int life, float travel, bool stone = false)
    {
        if (SpreadVfxBudget.Take(center)) new T_Facet_Particle(center, velocity, tint, size, life, travel, stone).Spawn();
    }
    internal static void Fire(Vector2 center, Vector2 velocity, float size, int life, float travel)
    {
        if (SpreadVfxBudget.Take(center)) new T_FireSmoke_Particle(center, velocity, size, life, travel).Spawn();
    }
    internal static void Facet(Vector2 center, float width, float height, float angle, Color tint, float opacity)
    {
        // Two differently lit solid facets, built in world pixels; no stretched white cross.
        Vector2 axis = angle.ToRotationVector2(), normal = axis.RotatedBy(MathHelper.PiOver2);
        const int slices = 12;
        for (int i = 0; i < slices; i++)
        {
            float t = (i + .5f) / slices;
            float w = width * (t < .32f ? t / .32f : (1f - t) / .68f);
            Vector2 p = center + axis * ((t - .5f) * height);
            Segment(p - normal * w * .5f, p, tint * opacity * .45f, height / slices + .5f);
            Segment(p, p + normal * w * .5f, tint * opacity, height / slices + .5f);
        }
    }
    internal static void Segment(Vector2 a, Vector2 b, Color tint, float width)
    {
        Vector2 d = b - a;
        if (!OperatorFlightVfx.Finite(d) || d.LengthSquared() > 360f * 360f || d.LengthSquared() < .001f) return;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            tint, d.ToRotation(), new Vector2(0f, .5f), new Vector2(d.Length(), MathHelper.Clamp(width, .5f, 12f)), SpriteEffects.None, 0f);
    }
    internal static void Arc(Vector2 center, float radius, float start, float sweep, Color tint, float width)
    {
        radius = MathHelper.Clamp(radius, 1f, 280f);
        const int count = 32;
        Vector2 previous = center + start.ToRotationVector2() * radius;
        for (int i = 1; i <= count; i++)
        {
            Vector2 next = center + (start + sweep * i / count).ToRotationVector2() * radius;
            Segment(previous, next, tint * MathF.Sin(MathHelper.Pi * (i - .5f) / count), width);
            previous = next;
        }
    }
}

// All motion is measured from an immutable emission point and clipped before drawing.
// These are Arknights Particle instances, registered only through Particle.Spawn().
