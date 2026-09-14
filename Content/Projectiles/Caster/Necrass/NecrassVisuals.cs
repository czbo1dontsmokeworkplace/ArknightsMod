using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

internal static class NecrassVisuals
{
    internal static readonly Color Violet = new(152, 84, 242);
    private static readonly Color Lilac = new(229, 208, 255);
    private static readonly Color Ash = new(68, 58, 86);
    internal static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * MathHelper.Clamp(opacity, 0, 1);
    internal static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (!float.IsFinite(length) || length < .01f || length > 900) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
            delta.ToRotation(), new Vector2(0, .5f), new Vector2(length, Math.Max(.5f, width)), SpriteEffects.None);
    }
    private static void Arc(Vector2 center, float radius, float start, float sweep, float squash, Color color, float width)
    {
        int steps = Math.Clamp((int)(radius * Math.Abs(sweep) / 9), 8, 100);
        Vector2 last = center + new Vector2(MathF.Cos(start), MathF.Sin(start) * squash) * radius;
        for (int i = 1; i <= steps; i++)
        {
            float angle = start + sweep * i / steps;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * squash) * radius;
            Line(last, next, color, width);
            last = next;
        }
    }
    private static Texture2D EffectTexture(string path) => ModContent.Request<Texture2D>("ArknightsMod/" + path).Value;
    private static void Stamp(Texture2D texture, Vector2 center, Vector2 size, float rotation, Color color)
    {
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }
    internal static void Seal(Vector2 center, float radius, float time, float opacity)
    {
        if (Main.dedServ || opacity <= 0 || radius <= 0) return;
        Texture2D ring = EffectTexture("Content/Textures/circle_03");
        Texture2D smoke = EffectTexture("Assets/GrayScaleTexture/Smoke2");
        float pulse = 1 + MathF.Sin(time * 2.1f) * .035f;
        // Elliptical light planes stay close to the ground; drifting smoke breaks their symmetry.
        Stamp(ring, center, new Vector2(radius * 2.25f, radius * .78f) * pulse,
            MathF.Sin(time * .3f) * .055f, Glow(Violet, opacity * .55f));
        Stamp(ring, center - new Vector2(0, 2), new Vector2(radius * 1.62f, radius * .49f),
            -MathF.Sin(time * .4f) * .07f, Glow(Lilac, opacity * .3f));
        for (int i = 0; i < 4; i++)
        {
            float angle = time * .6f + i * MathHelper.PiOver2;
            Vector2 orbit = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .3f) * radius * .43f;
            Stamp(smoke, center + orbit, new Vector2(radius * 1.05f, radius * .38f),
                MathF.Sin(angle) * .12f, Glow(Violet, opacity * .16f));
        }
    }
    internal static void Crown(Vector2 center, float radius, float time, float opacity, int jewels)
    {
        float bob = MathF.Sin(time * 2) * 2;
        center.Y += bob;
        Vector2 last = center + new Vector2(-radius, 4);
        for (int i = 0; i < 7; i++)
        {
            Vector2 next = center + new Vector2(-radius + radius * i / 3, i % 2 == 0 ? -radius * .5f : 2);
            Line(last, next, Glow(Violet, opacity * .35f), 5);
            Line(last, next, Glow(Lilac, opacity * .85f), 1);
            last = next;
        }
        Line(last, center + new Vector2(radius, 4), Glow(Violet, opacity), 2);
        Arc(center + new Vector2(0, 4), radius, 0, MathHelper.Pi, .23f, Glow(Violet, opacity), 1);
        for (int i = 0; i < Math.Min(6, jewels); i++)
        {
            Vector2 p = center + new Vector2((i - (jewels - 1) * .5f) * 5, 8);
            Line(p - Vector2.UnitY * 2, p + Vector2.UnitY * 2, Glow(Lilac, opacity), 2);
        }
    }
    internal static void Health(Vector2 center, float ratio, float width)
    {
        Line(center - new Vector2(width / 2, 0), center + new Vector2(width / 2, 0), new Color(20, 14, 28), 4);
        Line(center - new Vector2(width / 2, 0), center + new Vector2(width * (Math.Clamp(ratio, 0, 1) - .5f), 0), Violet, 2);
    }
    internal static void Bolt(Projectile projectile)
    {
        for (int i = projectile.oldPos.Length - 1; i > 0; i--)
        {
            if (projectile.oldPos[i] == Vector2.Zero || projectile.oldPos[i - 1] == Vector2.Zero) continue;
            float fade = 1 - i / (float)projectile.oldPos.Length;
            Vector2 a = projectile.oldPos[i] + projectile.Size / 2, b = projectile.oldPos[i - 1] + projectile.Size / 2;
            Line(a, b, new Color(24, 12, 39) * fade, 11 * fade);
            Vector2 side = (b - a).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2)
                * MathF.Sin(i * .55f + Main.GlobalTimeWrappedHourly * 12) * (1 - fade) * 9;
            Line(a + side, b + side, Glow(Violet, fade * .8f), 4 * fade);
            Line(a, b, Glow(Lilac, fade * .65f), 1.5f * fade);
        }
        Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 sideAxis = forward.RotatedBy(MathHelper.PiOver2);
        Vector2 center = projectile.Center;
        Line(center - forward * 10, center + forward * 11, Glow(Violet, 1), 6);
        Line(center - forward * 4, center + forward * 11, Glow(Lilac, 1), 2);
        Line(center - sideAxis * 6 - forward * 3, center + forward * 11, Glow(Lilac, .85f), 1);
        Line(center + sideAxis * 6 - forward * 3, center + forward * 11, Glow(Lilac, .85f), 1);
    }
    internal static void Impact(Vector2 center, Vector2 direction, float progress, float radius, int mode)
    {
        float fade = MathF.Pow(1 - MathHelper.Clamp(progress, 0, 1), 1.5f);
        float spread = 1 - MathF.Pow(1 - progress, 3);
        if (Main.dedServ) return;
        if (mode == 2)
        {
            Texture2D body = EffectTexture("Content/Projectiles/Guard/Hellagur/HellagurSlashBody");
            Texture2D edge = EffectTexture("Content/Projectiles/Guard/Hellagur/HellagurSlashEdge");
            Vector2 axis = direction.SafeNormalize(Vector2.UnitX);
            Vector2 bladeCenter = center + axis * radius * .53f;
            // One continuous crescent, with a soft violet body and a narrow pale cutting edge.
            // Body faces left; edge faces right. Align their bright rims before layering.
            float rotation = axis.ToRotation() + MathHelper.Pi + (progress - .15f) * .34f;
            float opening = .88f + Math.Min(progress * 2, .22f);
            Stamp(body, bladeCenter, new Vector2(radius * 1.05f, radius * 1.48f) * opening,
                rotation, Glow(Violet, fade * .7f));
            Stamp(edge, bladeCenter + axis * 3, new Vector2(radius * .97f, radius * 1.38f) * opening,
                rotation - MathHelper.Pi, Glow(Lilac, fade * .62f));
            return;
        }
        float r = Math.Max(5, radius * (.15f + spread * .85f));
        Seal(center, r, progress * 2, fade);
        if (mode == 4) { Crown(center + new Vector2(0, -10), 12, progress, fade, 0); return; }
        Texture2D smoke = EffectTexture("Assets/GrayScaleTexture/Smoke3");
        Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        // Billowing ash replaces the radial line spokes; each plume expands and turns separately.
        for (int i = 0; i < 5; i++)
        {
            float angle = i * MathHelper.TwoPi / 5 + .38f;
            Vector2 plume = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .6f) * r * .48f;
            plume.Y -= progress * radius * .22f;
            Vector2 size = new Vector2(r * .95f, r * .72f);
            Stamp(smoke, plume, size, angle + progress * .65f, new Color(35, 22, 49) * (fade * .3f));
            Stamp(smoke, plume - new Vector2(0, 3), size * .9f, angle + progress * .65f,
                Glow(Violet, fade * .24f));
        }
        Stamp(glow, center, new Vector2(r * 1.5f, r * .78f), 0, Glow(Violet, fade * .35f));
        Stamp(glow, center, new Vector2(r * .55f, r * .32f), 0, Glow(Lilac, fade * .35f));
        if (mode is 1 or 5) Crown(center - new Vector2(0, r * .35f), r * .4f, progress, fade, 6);
    }
    internal static void Embers(Vector2 center, int count, float speed)
    {
        if (Main.dedServ || Vector2.DistanceSquared(center, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2) > 1800 * 1800) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * .8f;
            if (ParticleManager.activeParticles.Count < 1400)
                new DefaultParticle(center, velocity, Main.rand.Next(24, 42), Main.rand.NextFloat(.2f, .48f),
                    i % 3 == 0 ? Lilac : Violet, true) { Deformation = new Vector2(.3f, 1.1f) }.Spawn();
            if (i % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(10, 10), DustID.Smoke,
                    velocity * .4f, 150, Ash, Main.rand.NextFloat(.7f, 1.15f));
                dust.noGravity = true;
            }
        }
    }
}
