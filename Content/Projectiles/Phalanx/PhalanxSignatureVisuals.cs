using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

// A purely visual addition to the existing shield/charge/shockwave. No gameplay projectiles.
public sealed class PhalanxSignatureVisuals : ModSystem
{
    private static Texture2D grain, cloud, glass;
    internal const int MaterialSize = 64;

    internal static Color[] BuildMaterial(int kind)
    {
        var pixels = new Color[MaterialSize * MaterialSize];
        for (int y = 0; y < MaterialSize; y++)
        for (int x = 0; x < MaterialSize; x++)
        {
            float u = (x + .5f) / MaterialSize * 2 - 1;
            float v = (y + .5f) / MaterialSize * 2 - 1;
            float radius = MathF.Sqrt(u * u + v * v);
            float noise = .5f + .25f * MathF.Sin(u * 19 + MathF.Cos(v * 17))
                + .25f * MathF.Sin(v * 31 - u * 11);
            float alpha, light;
            if (kind == 2)
            {
                // Asymmetric solid shard, with internal facets rather than wire outlines.
                float edge = 1 - Math.Abs(u + v * .22f) * 1.8f - Math.Abs(v) * .92f;
                alpha = MathHelper.Clamp(edge * 22, 0, 1) * .86f;
                light = u > v * .25f ? .96f : .46f;
                if (Math.Abs(u - v * .25f) < .06f) light = 1;
            }
            else
            {
                alpha = MathHelper.Clamp(1 - radius, 0, 1);
                alpha = kind == 0 ? MathHelper.Clamp(alpha * 5, 0, 1) : alpha * alpha;
                alpha *= .45f + noise * .55f;
                light = kind == 0 ? .7f + noise * .3f : 1;
            }
            pixels[y * MaterialSize + x] = new Color(light * alpha, light * alpha, light * alpha, alpha);
        }
        return pixels;
    }

    internal static void EnsureMaterials(GraphicsDevice device)
    {
        if (grain != null) return;
        grain = Make(device, 0); cloud = Make(device, 1); glass = Make(device, 2);
    }
    private static Texture2D Make(GraphicsDevice device, int kind)
    {
        var texture = new Texture2D(device, MaterialSize, MaterialSize);
        texture.SetData(BuildMaterial(kind));
        return texture;
    }
    public override void Unload()
    {
        Texture2D a = grain, b = cloud, c = glass;
        grain = cloud = glass = null;
        Main.QueueMainThreadAction(() => { a?.Dispose(); b?.Dispose(); c?.Dispose(); });
    }
    private static float Hash(int i)
    {
        float h = MathF.Sin(i * 127.1f + 31.7f) * 43758.5453f;
        return h - MathF.Floor(h);
    }
    private static float Ease(float t) => t * t * (3 - 2 * t);
    private static void Particle(Texture2D texture, Vector2 center, Vector2 size, Color tint, float opacity, float rotation = 0)
        => PhalanxVisuals.Sprite(texture, center, size, tint * MathHelper.Clamp(opacity, 0, 1), rotation);

    public static void DrawCharge(Vector2 center, Vector2 focus, int tier, float progress, float time)
    {
        if (Main.dedServ || progress <= 0) return;
        if (grain == null) EnsureMaterials(Main.instance.GraphicsDevice);
        progress = MathHelper.Clamp(progress, 0, 1);
        float visibility = Math.Min(1, progress * 5);
        if (tier == 0)
        {
            // Three loose fountains of sand rise from below the caster toward the staff.
            for (int i = 0; i < 66; i++)
            {
                float t = (progress * 1.8f + Hash(i)) % 1;
                Vector2 start = center + new Vector2((i % 3 - 1) * 44 + (Hash(i + 97) - .5f) * 22, 36);
                Vector2 p = Vector2.Lerp(start, focus, Ease(t));
                p.X += MathF.Sin(t * 7 + i) * 9 * MathF.Sin(t * MathHelper.Pi);
                float opacity = MathF.Sin(t * MathHelper.Pi) * visibility;
                Color tint = i % 4 == 0 ? new Color(255, 249, 205) : new Color(220, 175, 79);
                Particle(grain, p, new Vector2(2 + Hash(i + 11) * 4), tint, opacity, i + t);
                if (i % 8 == 0) Particle(cloud, p, new Vector2(26, 18), tint, opacity * .32f);
            }
        }
        else if (tier == 1)
        {
            // Two counter-rotating banks of red sand compress into a dense, unstable knot.
            for (int i = 0; i < 80; i++)
            {
                float t = (progress * 2.3f + Hash(i)) % 1;
                float sign = i % 2 == 0 ? 1 : -1;
                float a = sign * (t * 5.5f + time * 1.4f) + i % 5 * MathHelper.TwoPi / 5;
                Vector2 p = focus + a.ToRotationVector2() * (12 + 84 * (1 - Ease(t))) * new Vector2(1, .68f);
                float opacity = MathF.Sin(t * MathHelper.Pi) * visibility;
                Color tint = i % 3 == 0 ? new Color(57, 25, 35) : new Color(224, 75, 43);
                Particle(grain, p, new Vector2(3 + Hash(i + 18) * 4), tint, opacity, a);
                if (i % 7 == 0) Particle(cloud, p, new Vector2(25, 18), new Color(161, 44, 31), opacity * .6f, a);
            }
        }
        else
        {
            // Glass assembles in three staggered tiers; each shard flips to catch the light.
            for (int i = 0; i < 24; i++)
            {
                int layer = i / 8;
                float t = MathHelper.Clamp((progress - layer * .13f) / .6f, 0, 1);
                float a = i % 8 * MathHelper.TwoPi / 8 + layer * .29f + time * .18f;
                float radius = MathHelper.Lerp(88 + layer * 8, 19 + layer * 12, Ease(t));
                Vector2 p = focus + a.ToRotationVector2() * radius * new Vector2(1, .7f);
                Color tint = i % 4 == 0 ? new Color(198, 235, 246) : i % 2 == 0 ? new Color(197, 145, 239) : new Color(246, 183, 221);
                float flip = .35f + .65f * Math.Abs(MathF.Cos(time * 2 + i));
                Particle(glass, p, new Vector2((9 + t * 5) * flip, 19 + t * 8), tint, t * .85f, a + MathHelper.PiOver2);
                if (t > .8f) Particle(cloud, p, new Vector2(11), new Color(242, 232, 255), (t - .8f) * 1.5f);
            }
        }
    }

    public static void DrawRelease(Vector2 center, int tier, float progress, float maxRadius, bool powerful)
    {
        if (Main.dedServ || progress < 0 || progress >= 1 || !float.IsFinite(maxRadius)) return;
        if (grain == null) EnsureMaterials(Main.instance.GraphicsDevice);
        maxRadius = MathHelper.Clamp(maxRadius, 1, 600);
        float fade = (1 - progress) * Math.Min(1, progress * 12 + .2f);
        float power = powerful ? 1 : .65f;
        if (tier == 0)
        {
            // Sand blooms in low scalloped banks, then the grains lift and fall apart.
            for (int i = 0; i < 120; i++)
            {
                float a = i * 2.399963f;
                float r = maxRadius * MathF.Sqrt(progress) * (.35f + Hash(i) * .55f);
                Vector2 p = center + a.ToRotationVector2() * r;
                p.Y -= MathF.Sin(progress * MathHelper.Pi) * (12 + Hash(i + 31) * 27);
                Color tint = i % 4 == 0 ? new Color(255, 244, 188) : new Color(207, 157, 70);
                Particle(grain, p, new Vector2(3 + Hash(i + 29) * 5), tint, fade * power, a + progress * 2);
                if (i % 8 == 0) Particle(cloud, p + new Vector2(0, 9), new Vector2(54, 30) * (1 + progress), tint, fade * power * .48f, a * .1f);
            }
        }
        else if (tier == 1)
        {
            // Five ragged rotating lobes tear outward. No connecting lines or radial beams.
            for (int i = 0; i < 140; i++)
            {
                float trail = Hash(i);
                float t = MathHelper.Clamp(progress * 1.5f - trail * .35f, 0, 1);
                float a = i % 5 * MathHelper.TwoPi / 5 + t * 2.7f + trail * .5f;
                float r = maxRadius * (.08f + .82f * Ease(t));
                Vector2 p = center + a.ToRotationVector2() * r + new Vector2(MathF.Sin(i * 3), MathF.Cos(i * 5)) * (4 + 12 * progress);
                Color tint = i % 3 == 0 ? new Color(47, 24, 31) : i % 4 == 0 ? new Color(255, 171, 81) : new Color(227, 62, 41);
                Particle(grain, p, new Vector2(4 + trail * 5), tint, fade * power, a + t * 3);
                if (i % 7 == 0) Particle(cloud, p, new Vector2(48, 30), new Color(176, 45, 34), fade * power * .65f, a);
            }
        }
        else
        {
            // Three staggered shatters: broad glass facets, then small chips. Visual only.
            for (int i = 0; i < 60; i++)
            {
                int layer = i % 3;
                float t = MathHelper.Clamp((progress - layer * .085f) / (1 - layer * .085f), 0, 1);
                if (t <= 0) continue;
                float a = i * 2.399963f + layer * .21f;
                float r = (15 + maxRadius * .86f * MathF.Sqrt(t)) * (.6f + Hash(i) * .4f);
                Vector2 p = center + a.ToRotationVector2() * r;
                Color tint = i % 5 == 0 ? new Color(197, 234, 255) : i % 2 == 0 ? new Color(185, 130, 231) : new Color(248, 186, 232);
                float flip = .3f + .7f * Math.Abs(MathF.Cos(t * 8 + i));
                Vector2 size = new Vector2((7 + Hash(i) * 8) * flip, 15 + Hash(i + 71) * 17) * (1 - t * .5f);
                Particle(glass, p, size, tint, (1 - t) * power, a + t * (layer - 1) * 4);
                if (i % 3 == 0) Particle(cloud, p, new Vector2(15), new Color(240, 214, 255), (1 - t) * power * .45f);
            }
        }
    }

    public static void Hit(Vector2 center, int tier)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 12; i++)
        {
            Vector2 velocity = tier == 0 ? new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-5, -1))
                : Main.rand.NextVector2CircularEdge(tier == 1 ? 7 : 4, tier == 1 ? 7 : 4);
            int dustType = tier == 0 ? DustID.Sand : tier == 1 ? DustID.SandstormInABottle : DustID.GemAmethyst;
            Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 80, PhalanxVisuals.Palette(tier), tier == 1 ? 1.3f : 1);
            dust.noGravity = tier != 0;
        }
    }
}
