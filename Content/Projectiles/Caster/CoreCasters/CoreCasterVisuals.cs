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
    internal static void Trail(Projectile p, CoreCasterKind kind, int mode = 0)
    {
        if (!Visible(p.Center)) return;
        Vector2 direction = p.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
        int palette = kind == CoreCasterKind.Haze && mode == 2 ? CoreCasterArt.CrimsonPalette : 0;
        float phase = Main.GlobalTimeWrappedHourly * 27 + p.identity;
        Vector2 orbit = side * MathF.Sin(phase) * (6 + MathF.Cos(phase * .7f) * 2);
        Add(p.Center + orbit, -direction * .6f + side * MathF.Cos(phase) * .45f,
            kind, 16, kind == CoreCasterKind.Durin ? 5 : 7, 6 + palette);
        if (Main.rand.NextBool(2)) Add(p.Center, -direction * Main.rand.NextFloat(.3f, 1.1f), kind, 10, 8, 5 + palette);
        int variant = kind == CoreCasterKind.Eyjafjalla ? 3 : kind is CoreCasterKind.Haze or CoreCasterKind.Nightmare ? 4 : 0;
        Add(p.Center - orbit * .7f, -direction * .9f - Vector2.UnitY * .12f,
            kind, 20, kind == CoreCasterKind.Eyjafjalla ? 14 : 9, variant + palette);
        if (kind == CoreCasterKind.Eyjafjalla && Main.rand.NextBool(4))
            Add(p.Center, -direction * .15f - Vector2.UnitY * .2f, kind, 24, 20, 1);
    }
    internal static void Muzzle(Vector2 at, Vector2 direction, CoreCasterKind kind, bool empowered)
    {
        Add(at, direction * .2f, kind, 12, empowered ? 19 : 12, 2);
        for (int i = 0; i < (empowered ? 3 : 1); i++)
            Add(at, direction.RotatedByRandom(.4f) * Main.rand.NextFloat(1, 3), kind, 14, 9, 0);
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
        Add(at, Vector2.Zero, kind, 22, empowered ? 31 : 22, 2 + palette);
        int count = kind == CoreCasterKind.Eyjafjalla ? 4 : empowered ? 8 : 5;
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < count; i++)
            Add(at, direction * .7f + Main.rand.NextVector2Circular(2.8f, 2.8f), kind, Main.rand.Next(13, 23),
                Main.rand.NextFloat(8, 15), (i % 3 == 0 ? 5 : 0) + palette);
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
