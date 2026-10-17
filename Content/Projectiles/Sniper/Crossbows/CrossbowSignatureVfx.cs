using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using MoteMaterial = ArknightsMod.Common.Particle.T.T_Crossbow_Mote_Particle.Material;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// The shaft and its historical silhouettes carry the flight. Free particles describe the
// material shed by each weapon, rather than rebuilding a rigid ornament around every arrow.
internal static class CrossbowSignatureVfx
{
    private readonly record struct FlightStyle(Color Body, Color Edge, float Reach, float Width, int Echoes);
    private static FlightStyle Style(CrossbowKind kind) => kind switch {
        CrossbowKind.Kroos => new(new(190, 115, 43), new(255, 218, 153), 52f, 2.2f, 3),
        CrossbowKind.KroosAlter => new(new(61, 160, 156), new(193, 255, 235), 80f, 3f, 4),
        CrossbowKind.Melanite => new(new(115, 48, 205), new(226, 182, 255), 130f, 4.6f, 6),
        CrossbowKind.Provence => new(new(137, 76, 173), new(255, 211, 150), 110f, 5.2f, 5),
        CrossbowKind.Schwarz => new(new(52, 24, 77), new(202, 158, 242), 165f, 5.7f, 7),
        CrossbowKind.GreyThroat => new(new(111, 162, 197), new(229, 249, 255), 74f, 2.4f, 4),
        CrossbowKind.BluePoison => new(new(39, 149, 112), new(156, 248, 199), 78f, 3.1f, 4),
        _ => new(new(180, 64, 117), new(255, 204, 227), 110f, 3.8f, 5) };
    internal static bool Handles(CrossbowKind kind) => (uint)kind <= (uint)CrossbowKind.BluePoison;
    internal static bool Heavy(CrossbowKind kind) => kind is CrossbowKind.Melanite or CrossbowKind.Provence or CrossbowKind.Schwarz;
    internal static Color Tint(CrossbowKind kind) => Style(kind).Body;
    private static Texture2D NativeArrow(CrossbowKind kind)
    {
        if (kind == CrossbowKind.Schwarz)
            return ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/Schwarz/SchwarzArrow",
                AssetRequestMode.ImmediateLoad).Value;
        if (kind == CrossbowKind.Pozemka)
            return ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/Pozemka/PozemkaCrossbow_Projectile",
                AssetRequestMode.ImmediateLoad).Value;
        Main.instance.LoadProjectile(ProjectileID.WoodenArrowFriendly);
        return TextureAssets.Projectile[ProjectileID.WoodenArrowFriendly].Value;
    }
    private static Vector2 CoreSize(Texture2D texture, CrossbowKind kind) => kind switch {
        CrossbowKind.Schwarz => new(8f, 54f), CrossbowKind.Pozemka => new(27f, 27f),
        CrossbowKind.Melanite => texture.Size() * new Vector2(1.12f, 1.15f),
        CrossbowKind.Provence => texture.Size() * new Vector2(1.22f, 1.16f), _ => texture.Size() };
    private static float Rotation(float angle, CrossbowKind kind)
        => angle + (kind is CrossbowKind.Pozemka or CrossbowKind.Schwarz ? -MathHelper.PiOver2 : MathHelper.PiOver2);
    private static void Sprite(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation)
        => Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    private static Color Light(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;
    private static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    private static void DrawCore(Projectile p, CrossbowKind kind, float opacity)
    {
        Texture2D texture = NativeArrow(kind);
        Vector2 size = CoreSize(texture, kind);
        FlightStyle style = Style(kind);
        float angle = Rotation(p.velocity.ToRotation(), kind);
        opacity *= p.Opacity;
        // A dark silhouette survives on bright terrain; subpixel coloured edges survive underground.
        Sprite(texture, p.Center, size * 1.11f, new Color(19, 17, 26) * (opacity * .7f), angle);
        float border = Heavy(kind) ? .9f : .65f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * border;
            Sprite(texture, p.Center + offset, size, Light(style.Edge, opacity * .26f), angle);
        }
        Sprite(texture, p.Center, size, Color.Lerp(Color.White, style.Edge, .16f) * opacity, angle);
        Sprite(texture, p.Center, size, Light(style.Edge, opacity * .14f), angle);
    }
    internal static void DrawWoodenArrow(Projectile p, float opacity = 1f)
    {
        if (!Main.dedServ) DrawCore(p, CrossbowKind.Kroos, opacity);
    }

    // Histories may be projectile top-left positions or already-centred custom histories.
    // Crop by world distance so extraUpdates and different ammunition cannot change trail reach.
    private static int Path(Projectile p, ReadOnlySpan<Vector2> history, Vector2 offset,
        Span<Vector2> path, float reach)
    {
        if (!Finite(p.Center)) return 0;
        path[0] = p.Center;
        int count = 1;
        float traveled = 0f;
        foreach (Vector2 saved in history)
        {
            if (saved == Vector2.Zero || !Finite(saved)) break;
            Vector2 next = saved + offset;
            float step = Vector2.Distance(path[count - 1], next);
            if (step > 160f) break;
            if (step < .5f) continue;
            float remaining = reach - traveled;
            path[count] = step > remaining ? Vector2.Lerp(path[count - 1], next, remaining / step) : next;
            count++;
            traveled += Math.Min(step, remaining);
            if (traveled >= reach || count == path.Length) break;
        }
        return count;
    }
    private static void DrawFlight(Projectile p, CrossbowKind kind, ReadOnlySpan<Vector2> history,
        Vector2 offset, bool drawArrow, float opacity = 1f, bool texturedWake = true)
    {
        if (Main.dedServ || !Finite(p.Center)) return;
        FlightStyle style = Style(kind);
        Span<Vector2> path = stackalloc Vector2[32];
        int count = Path(p, history, offset, path, style.Reach);
        float total = 0f;
        for (int i = 1; i < count; i++) total += Vector2.Distance(path[i - 1], path[i]);
        Texture2D arrow = NativeArrow(kind);
        Vector2 size = CoreSize(arrow, kind);
        // Discrete, recognisable arrow silhouettes, spaced by distance rather than update count.
        if (total > 3f)
        {
            int segment = 1;
            float consumed = 0f;
            int echoes = Math.Min(style.Echoes, Math.Max(1, (int)(total / 9f)));
            for (int i = 1; i <= echoes; i++)
            {
                float t = i / (float)(echoes + 1);
                float distance = total * t;
                float span = Vector2.Distance(path[segment - 1], path[segment]);
                while (segment < count - 1 && consumed + span < distance)
                {
                    consumed += span; segment++;
                    span = Vector2.Distance(path[segment - 1], path[segment]);
                }
                Vector2 center = Vector2.Lerp(path[segment - 1], path[segment],
                    MathHelper.Clamp((distance - consumed) / Math.Max(.001f, span), 0f, 1f));
                float angle = Rotation((path[segment - 1] - path[segment]).ToRotation(), kind);
                float fade = MathF.Pow(1f - t, 1.65f) * opacity * p.Opacity;
                Sprite(arrow, center, size * (1f - t * .15f),
                    Light(Color.Lerp(style.Edge, style.Body, t), fade * (Heavy(kind) ? .48f : .34f)), angle);
            }
        }
        using (var pass = new CrossbowMaterialPass())
        {
            float traveled = 0f;
            for (int i = 1; texturedWake && i < count; i++)
            {
                Vector2 delta = path[i - 1] - path[i];
                float length = delta.Length();
                float t = (traveled + length * .5f) / Math.Max(1f, total);
                float fade = MathF.Pow(1f - t, 1.7f) * opacity * p.Opacity;
                Vector2 at = (path[i - 1] + path[i]) * .5f;
                // Soft textured streaks follow the real path, with no fixed rails or side ribs.
                pass.Add(CrossbowMaterial.SoftSpark, at,
                    new(length + 7f, style.Width * (1f - t * .65f)),
                    Color.Lerp(style.Edge, style.Body, Math.Min(1f, t * 1.7f)), delta.ToRotation(), fade * .48f);
                if (kind == CrossbowKind.Schwarz)
                    pass.Add(CrossbowMaterial.Smear, at, new(length + 12f, 7f * (1f - t)),
                        new(24, 12, 34), delta.ToRotation(), fade * .5f, false);
                traveled += length;
            }
            Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
            if (kind != CrossbowKind.Kroos)
                pass.Add(CrossbowMaterial.Flux, p.Center + aim * (kind == CrossbowKind.Schwarz ? 21f : 8f),
                    new(Heavy(kind) ? 12f : 7f, Heavy(kind) ? 8f : 5f), style.Edge,
                    aim.ToRotation(), opacity * p.Opacity * .45f);
        }
        if (drawArrow) DrawCore(p, kind, opacity);
    }
    internal static void Draw(Projectile p, CrossbowKind kind, Vector2[] history, bool drawArrow)
        => DrawFlight(p, kind, history ?? p.oldPos, history == null ? p.Size * .5f : Vector2.Zero, drawArrow);
    internal static void DrawRoseSatellite(Projectile p, ReadOnlySpan<Vector2> history, float opacity)
        => DrawFlight(p, CrossbowKind.Pozemka, history, Vector2.Zero, true, opacity, false);
    internal static void Wake(Projectile p, CrossbowKind kind)
    {
        if (!Main.dedServ && p.numUpdates == 0)
            p.GetGlobalProjectile<CrossbowShotData>().FlightEmitter.Tick(p, kind);
    }
    internal static void DrawMuzzle(Vector2 center, Vector2 aim, CrossbowKind kind, float strength)
    {
        if (Main.dedServ || strength <= 0f) return;
        FlightStyle style = Style(kind);
        strength = MathHelper.Clamp(strength, 0f, 1f);
        using var pass = new CrossbowMaterialPass();
        pass.Add(CrossbowMaterial.SoftSpark, center + aim * 8f,
            new((Heavy(kind) ? 38f : 22f) * strength, (Heavy(kind) ? 12f : 6f) * strength),
            style.Edge, aim.ToRotation(), strength * .65f);
        pass.Add(CrossbowMaterial.Flux, center, new Vector2(Heavy(kind) ? 18f : 9f) * strength,
            style.Body, 0f, strength * .25f);
    }
    internal static void Pulse(Projectile p, CrossbowKind kind, bool muzzle)
    {
        if (!muzzle) { Hit(p, kind, .65f); return; }
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        FlightStyle style = Style(kind);
        int count = Heavy(kind) ? 6 : kind == CrossbowKind.Kroos ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = aim.RotatedByRandom(.6f) * Main.rand.NextFloat(1.2f, 4.5f);
            CrossbowParticles.Spark(p.Center + Main.rand.NextVector2Circular(3f, 3f), velocity,
                i % 3 == 0 ? style.Edge : style.Body, Heavy(kind) ? 24f : 13f,
                Heavy(kind) ? 4f : 2.4f, Main.rand.Next(7, 12));
        }
        if (Heavy(kind))
            CrossbowParticles.Wisp(p.Center, -aim * .7f, style.Body, 22f, 19, .2f);
        if (kind == CrossbowKind.BluePoison)
            CrossbowParticles.Mote(p.Center, aim * 1.5f + Main.rand.NextVector2Circular(1f, 1f),
                style.Body, 5f, 15, MoteMaterial.Venom);
    }
    internal static void Hit(Projectile p, CrossbowKind kind, float strength = 1f)
    {
        if (Main.dedServ || !Handles(kind)) return;
        CrossbowParticleCompositions.Impact(p, kind, strength);
    }
}

internal sealed class CrossbowFlightEmitter
{
    private Vector2 previous;
    private bool initialized;
    private int frame;
    private ulong lastTick = ulong.MaxValue;
    internal int Frame => frame;
    internal void Tick(Projectile p, CrossbowKind kind)
    {
        if (Main.dedServ || p.numUpdates != 0 || lastTick == Main.GameUpdateCount) return;
        lastTick = Main.GameUpdateCount;
        Vector2 center = p.Center;
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y)) return;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        if (!initialized || Vector2.DistanceSquared(previous, center) > 180f * 180f)
        { previous = center; initialized = true; frame = 0; }
        CrossbowParticleCompositions.Flight(p, kind, previous, frame);
        if (kind == CrossbowKind.Melanite)
            MelaniteFluxVfx.Emit(p, previous, aim, frame);
        else
        {
            // Material-specific fine debris complements the larger energy grains.
            // Subupdates never multiply the emission count.
            int samples = CrossbowSignatureVfx.Heavy(kind) ? 2 : 1;
            for (int i = 0; i < samples; i++)
            {
                Vector2 at = Vector2.Lerp(previous, center, (i + .5f) / samples) - aim * 6f;
                Vector2 drift = -aim * Main.rand.NextFloat(.35f, 1.4f) + Main.rand.NextVector2Circular(.7f, .7f);
                Color tint = CrossbowSignatureVfx.Tint(kind);
                switch (kind)
                {
                    case CrossbowKind.Kroos:
                        if ((frame + p.identity) % 5 == 0)
                            CrossbowParticles.Mote(at, drift, new(232, 180, 106), 3f, 10, MoteMaterial.Fleck);
                        break;
                    case CrossbowKind.KroosAlter:
                        if ((frame + p.identity) % 3 == 0)
                            CrossbowParticles.Mote(at, drift, new(161, 241, 221), 4f, 13, MoteMaterial.Frost);
                        break;
                    case CrossbowKind.Provence:
                        CrossbowParticles.Spark(at, drift * 1.6f, i == 0 ? tint : new(255, 195, 121),
                            Main.rand.NextFloat(12f, 20f), 3.2f, 12, .05f);
                        if (frame % 5 == 0 && i == 0)
                            CrossbowParticles.Wisp(at, drift * .3f, new(103, 67, 124), 17f, 18, .18f);
                        break;
                    case CrossbowKind.Schwarz:
                        if (i == 0)
                            CrossbowParticles.Loose(CrossbowMaterial.Smear, at + Main.rand.NextVector2Circular(3f, 3f),
                                drift, new(43, 21, 58), new(27f, 7f), 12, aim.ToRotation(),
                                spin: Main.rand.NextFloat(-.035f, .035f), bright: false, opacity: .5f);
                        else if (frame % 2 == 0)
                            CrossbowParticles.Spark(at, drift, new(189, 140, 228), 17f, 2.5f, 9);
                        break;
                    case CrossbowKind.GreyThroat:
                        if ((frame + p.identity) % 3 == 0)
                            CrossbowParticles.Spark(at, drift, new(200, 231, 246), 11f, 2f, 7);
                        break;
                    case CrossbowKind.BluePoison:
                        if ((frame + p.identity) % 2 == 0)
                            CrossbowParticles.Mote(at, drift, frame % 4 == 0 ? new(77, 139, 217) : new(69, 207, 141),
                                Main.rand.NextFloat(3.5f, 5.5f), 15, MoteMaterial.Venom);
                        if ((frame + p.identity) % 7 == 0)
                            CrossbowParticles.Wisp(at, drift * .2f, new(46, 123, 98), 15f, 19, .22f);
                        break;
                    case CrossbowKind.Pozemka:
                        if ((frame + p.identity) % 3 == 0)
                            CrossbowParticles.Mote(at, drift, new(133, 44, 86), 5.5f, 14, MoteMaterial.Ink);
                        if ((frame + p.identity) % 5 == 0)
                            CrossbowParticles.Spark(at, drift, new(250, 183, 212), 12f, 2.5f, 9);
                        break;
                }
            }
        }
        previous = center; frame++;
    }
}
