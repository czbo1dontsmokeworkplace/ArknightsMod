using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

internal static class NecrassVisuals
{
    internal static readonly Color Violet = NecrassArt.Violet;
    private static readonly Color Lilac = NecrassArt.Lilac;
    internal static Color Glow(Color color, float opacity) => NecrassArt.Glow(color, opacity);

    internal static bool Ready()
    {
        if (Main.dedServ) return false;
        if (!NecrassArt.Ready)
            NecrassArt.Assign(name => ModContent.Request<Texture2D>(NecrassArt.Assets + name, AssetRequestMode.ImmediateLoad).Value);
        return true;
    }

    private static bool CanEmit(Vector2 point, int budget = 1050) => !Main.dedServ &&
        ParticleManager.activeParticles.Count < budget && Vector2.DistanceSquared(point,
            Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) < 1800 * 1800;

    // Straight geometry is reserved for the functional servant health bar.
    private static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        if (!float.IsFinite(length) || length < .01f || length > 900) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(length, width), SpriteEffects.None);
    }

    internal static void Seal(Vector2 center, float radius, float time, float opacity)
    {
        if (Ready()) NecrassArt.Ground(Main.spriteBatch, center - Main.screenPosition, radius, time, opacity);
    }

    internal static void Crown(Vector2 center, float radius, float time, float opacity, int jewels)
    {
        if (Ready()) NecrassArt.Crown(Main.spriteBatch, center - Main.screenPosition, radius, time, opacity, jewels);
    }

    internal static void Ritual(Vector2 center, float radius, float time, float opacity, int mode)
    {
        if (Ready()) NecrassArt.Ritual(Main.spriteBatch, center - Main.screenPosition, radius, time, opacity, mode);
    }

    internal static void Offering(Vector2 center, float time)
    {
        if (Ready()) NecrassArt.Flame(Main.spriteBatch, center - Main.screenPosition,
            new Vector2(.2f, -1), 22, time, 4);
    }

    internal static void Health(Vector2 center, float ratio, float width)
    {
        Line(center - new Vector2(width / 2, 0), center + new Vector2(width / 2, 0), new Color(20, 14, 28), 4);
        Line(center - new Vector2(width / 2, 0), center + new Vector2(width * (Math.Clamp(ratio, 0, 1) - .5f), 0), Violet, 2);
    }

    internal static void SoulHelix(Projectile projectile, float phase)
    {
        if (!CanEmit(projectile.Center)) return;
        Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
        Vector2 offset = side * MathF.Sin(phase) * 7 + forward * MathF.Cos(phase) * 3;
        new T_Necrass_SoulEmber(projectile.Center + offset,
            -forward * .45f + side * MathF.Sin(phase) * .35f,
            Lilac, Main.rand.Next(12, 20), Main.rand.NextFloat(6, 10), 0).Spawn();
    }

    internal static void SoulAsh(Vector2 center, Vector2 velocity, int count = 1)
    {
        Vector2 forward = velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < count && CanEmit(center); i++)
            new T_Necrass_Ash(center + Main.rand.NextVector2Circular(5, 5),
                forward * Main.rand.NextFloat(.1f, .35f) - Vector2.UnitY * .18f,
                Main.rand.Next(20, 33), Main.rand.NextFloat(12, 20)).Spawn();
    }

    internal static void LingeringWake(Vector2 center, Vector2 velocity)
    {
        if (!CanEmit(center)) return;
        Vector2 side = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        Vector2 point = center + side * Main.rand.NextFloat(-7, 7);
        Vector2 drift = Main.rand.NextVector2Circular(.3f, .3f) - Vector2.UnitY * Main.rand.NextFloat(.12f, .35f);
        // Short, detached curls remain in world space after this very fast bolt has passed.
        new T_Necrass_SoulEmber(point, drift, Violet, Main.rand.Next(25, 40), Main.rand.NextFloat(8, 13), 1).Spawn();
    }

    internal static void Bolt(Projectile projectile)
    {
        if (Ready()) NecrassArt.Bolt(Main.spriteBatch, projectile.Center - Main.screenPosition,
            projectile.velocity, projectile.oldPos, projectile.Size * .5f - Main.screenPosition,
            Main.GlobalTimeWrappedHourly, projectile.identity, (int)projectile.ai[0]);
    }

    internal static void Impact(Vector2 center, Vector2 direction, float progress, float radius, int mode)
    {
        if (Ready()) NecrassArt.Impact(Main.spriteBatch, center - Main.screenPosition, direction, progress, radius, mode);
    }

    internal static void Embers(Vector2 center, int count, float speed)
    {
        for (int i = 0; i < count && CanEmit(center, 1400); i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(speed, speed) - Vector2.UnitY * .8f;
            new T_Necrass_SoulEmber(center, velocity, i % 4 == 0 ? Lilac : Violet,
                Main.rand.Next(16, 28), Main.rand.NextFloat(9, 17), i % 4 == 0 ? 1 : 0).Spawn();
            if (i % 4 == 0 && CanEmit(center, 1400))
                new T_Necrass_Ash(center + Main.rand.NextVector2Circular(10, 10), velocity * .3f,
                    Main.rand.Next(24, 38), Main.rand.NextFloat(18, 30)).Spawn();
        }
    }
}
