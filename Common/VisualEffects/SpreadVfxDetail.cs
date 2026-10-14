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
        if (SpreadVfxBudget.Take(center)) new SpreadFacetParticle(center, velocity, tint, size, life, travel, stone).Spawn();
    }
    internal static void Fire(Vector2 center, Vector2 velocity, float size, int life, float travel, bool smoke = false)
    {
        if (SpreadVfxBudget.Take(center)) new SpreadFireParticle(center, velocity, size, life, travel, smoke).Spawn();
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
public sealed class SpreadFacetParticle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, launch;
    private readonly Color tint;
    private readonly float size, travel, spin;
    private readonly bool stone;
    public override string TexturePath => OperatorFlightVfx.Assets + "StarCore";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    internal SpreadFacetParticle(Vector2 center, Vector2 velocity, Color tint, float size, int life, float travel, bool stone)
    {
        Position = origin = center; launch = velocity; this.tint = tint;
        this.size = MathHelper.Clamp(size, 4, 30); Lifetime = Math.Clamp(life, 8, 40);
        this.travel = MathHelper.Clamp(travel, 6, 240); this.stone = stone;
        Rotation = velocity.ToRotation(); spin = Main.rand.NextFloat(-.1f, .1f);
    }
    public override void Update()
    {
        Vector2 offset = launch * ((1f - MathF.Pow(.93f, Time)) / .07f);
        if (stone) offset.Y += Time * Time * .018f;
        if (offset.LengthSquared() > travel * travel) offset = offset.SafeNormalize(Vector2.UnitY) * travel;
        Position = origin + offset; Rotation += spin;
        Color = tint * MathHelper.Clamp((Lifetime - Time) / 10f, 0f, 1f);
    }
    public override void Draw()
    {
        SpreadVfxDetail.Facet(Position, size * (stone ? .8f : .4f), size, Rotation, Color, 1f);
        if (!stone)
            SpreadVfxDetail.Segment(Position - Rotation.ToRotationVector2() * size * .26f,
                Position + Rotation.ToRotationVector2() * size * .34f,
                OperatorFlightVfx.Add(tint, .6f * (1f - LifetimeRatio)), 1f);
    }
}

public sealed class SpreadFireParticle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, launch;
    private readonly float size, travel, curl;
    private readonly bool smoke;
    public override string TexturePath => OperatorFlightVfx.Assets + "ForwardFlame";
    public override BlendState DrawBlendState => smoke ? BlendState.NonPremultiplied : BlendState.Additive;
    internal SpreadFireParticle(Vector2 center, Vector2 velocity, float size, int life, float travel, bool smoke)
    {
        Position = origin = center; launch = velocity; this.size = MathHelper.Clamp(size, 8, 70);
        Lifetime = Math.Clamp(life, 8, 40); this.travel = MathHelper.Clamp(travel, 8, 220);
        this.smoke = smoke; curl = Main.rand.NextFloat(-.55f, .55f);
        Rotation = velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void Update()
    {
        float t = LifetimeRatio;
        Vector2 offset = launch.RotatedBy(curl * t) * ((1f - MathF.Pow(.94f, Time)) / .06f) - Vector2.UnitY * Time * t * .35f;
        if (offset.LengthSquared() > travel * travel) offset = offset.SafeNormalize(-Vector2.UnitY) * travel;
        Position = origin + offset;
        Rotation += curl * .025f;
        float envelope = MathHelper.Clamp(Time / 3f, 0, 1) * MathF.Pow(1 - t, smoke ? .8f : 1.2f);
        Color = (smoke ? new Color(47, 32, 42) : Color.Lerp(new Color(255, 218, 113), new Color(235, 49, 17), t)) * envelope;
    }
    public override void Draw()
    {
        float t = LifetimeRatio;
        Vector2 dimensions = new(size * (.45f + .35f * t), size * (1f + t * .28f));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, dimensions / Texture.Size(), SpriteEffects.None, 0f);
    }
}
