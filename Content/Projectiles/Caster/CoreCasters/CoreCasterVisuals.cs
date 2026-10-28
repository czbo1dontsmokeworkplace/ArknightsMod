using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

internal static class CoreCasterVisuals
{
    internal static bool Ready()
    {
        if (Main.dedServ) return false;
        if (!CoreCasterArt.Ready) CoreCasterArt.Assign(n => ModContent.Request<Texture2D>(CoreCasterArt.Assets + n, AssetRequestMode.ImmediateLoad).Value);
        return true;
    }
    private static bool Visible(Vector2 at) => !Main.dedServ && ParticleManager.activeParticles.Count < 1100 &&
        Vector2.DistanceSquared(at, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2) < 1800 * 1800;
    private static void Add(Vector2 at, Vector2 drift, CoreCasterKind kind, int life, float size, int variant)
    {
        if (Visible(at)) new T_CoreCasterParticle(at, drift, kind, life, size, variant).Spawn();
    }
    internal static void Bolt(Projectile p, CoreCasterKind kind, int mode = 0)
    {
        if (!Ready()) return;
        CoreCasterTrail.Draw(p, kind, mode);
        CoreCasterArt.Bolt(Main.spriteBatch, p.Center - Main.screenPosition, p.velocity, p.oldPos,
            p.Size / 2 - Main.screenPosition, kind, mode, Main.GlobalTimeWrappedHourly, p.identity);
    }
    internal static void Retire(Projectile p, CoreCasterKind kind, int mode = 0)
    {
        if (Visible(p.Center)) new T_CoreCasterWake(p, kind, mode).Spawn();
    }
    internal static void Trail(Projectile p, CoreCasterKind kind, int mode = 0)
    {
        if (!Visible(p.Center)) return;
        Vector2 direction = p.velocity.SafeNormalize(Vector2.UnitX);
        int palette = kind == CoreCasterKind.Haze && mode == 2 ? CoreCasterArt.CrimsonPalette : 0;
        Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
        // Short detached luminous slivers: position belongs to the path, never an orbit around a ball.
        Add(p.Center + side * Main.rand.NextFloat(-2, 2), -direction * Main.rand.NextFloat(.15f, .65f),
            kind, 10, kind == CoreCasterKind.Absinthe ? 34 : 24, 7 + palette);
        if (Main.rand.NextBool(3)) Add(p.Center, -direction.RotatedByRandom(.3f) * Main.rand.NextFloat(.6f, 2), kind, 14, 10, 5 + palette);
        if (kind == CoreCasterKind.Eyjafjalla && Main.rand.NextBool(2))
            Add(p.Center - direction * 5, -direction * .4f - Vector2.UnitY * .25f, kind, 15, 17, 3);
    }
    internal static void Muzzle(Vector2 at, Vector2 direction, CoreCasterKind kind, bool empowered)
    {
        Add(at, direction * .01f, kind, 8, empowered ? 34 : 24, 8);
        for (int i = 0; i < (empowered ? 5 : 3); i++)
            Add(at, direction.RotatedByRandom(.38f) * Main.rand.NextFloat(2, 6), kind, 10, Main.rand.NextFloat(12, 23), 5);
    }
    internal static void Impact(Vector2 at, Vector2 velocity, CoreCasterKind kind, bool empowered = false, int mode = 0)
    {
        if (!Visible(at)) return;
        if (kind == CoreCasterKind.Eyjafjalla && empowered)
        {
            // The damaging splash owns the one fire bloom; do not stack a second flame cluster on every rapid hit.
            for (int i = 0; i < 3; i++) Add(at, Main.rand.NextVector2Circular(2.2f, 2.2f), kind, 15, 10, 0);
            return;
        }
        int palette = kind == CoreCasterKind.Haze && mode == 2 ? CoreCasterArt.CrimsonPalette : 0;
        Add(at, Vector2.Zero, kind, 12, empowered ? 34 : 23, 2 + palette);
        Add(at, velocity.SafeNormalize(Vector2.UnitX) * .01f, kind, 15, empowered ? 74 : 50, 8 + palette);
        int count = kind == CoreCasterKind.Eyjafjalla ? 7 : empowered ? 13 : 9;
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < count; i++)
            Add(at, (i % 3 == 0 ? Main.rand.NextVector2Unit() : direction.RotatedByRandom(1.15f)) * Main.rand.NextFloat(2, 8),
                kind, Main.rand.Next(10, 21), Main.rand.NextFloat(13, 28), 5 + palette);
        Add(at, -direction * .35f, kind, 22, empowered ? 34 : 22, 1 + palette);
    }
    internal static void Burst(Vector2 at, CoreCasterKind kind, float radius)
    {
        if (!Visible(at)) return;
        Add(at, Vector2.Zero, kind, 28, radius, 2);
        for (int i = 0; i < (radius > 65 ? 14 : 7); i++)
        {
            Add(at, Main.rand.NextVector2Circular(5, 5) - Vector2.UnitY, kind, Main.rand.Next(16, 29), Main.rand.NextFloat(10, 20), 0);
            if (kind == CoreCasterKind.Eyjafjalla && i % 3 == 0)
                Add(at + Main.rand.NextVector2Circular(radius * .25f, radius * .15f), Main.rand.NextVector2Circular(.7f, .7f) - Vector2.UnitY,
                    kind, 36, radius * .7f, 1);
        }
    }
    internal static void Activate(Vector2 at, CoreCasterKind kind) => Burst(at, kind, kind == CoreCasterKind.Eyjafjalla ? 60 : 42);
    internal static void Mend(Vector2 at, Vector2 velocity) => Add(at, -velocity.SafeNormalize(Vector2.UnitX) * .2f,
        CoreCasterKind.Nightmare, 16, 7, 5);
    internal static void WindMote(Vector2 at, Vector2 velocity, float scale, bool spark) =>
        Add(at, velocity, CoreCasterKind.Hoolheyak, spark ? 19 : 24, (spark ? 7 : 13) * scale, spark ? 5 : 4);
    internal static void Brand(Vector2 center, float radius, float time, float opacity)
    {
        if (Ready()) CoreCasterArt.Brand(Main.spriteBatch, center - Main.screenPosition, radius, time, opacity);
    }
}
