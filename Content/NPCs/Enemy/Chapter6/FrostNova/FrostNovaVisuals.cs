using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// All dimensions in this renderer are final world pixels. Existing FrostNova art
// stays the silhouette; the caster's graded materials supply frost, flow and facets.
internal static class FrostNovaVisuals
{
    internal const string Assets = "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/";
    internal static readonly Color Ice = new(84, 180, 229);
    internal static readonly Color Pale = new(196, 244, 255);
    private static readonly Vector2[] path = new Vector2[30];
    private static readonly float[] distance = new float[30];
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[60];
    private static Effect trail;
    private static Texture2D flow;
    private static readonly Dictionary<string, Texture2D> textures = new(StringComparer.Ordinal);

    internal static bool Visible(Vector2 at, float margin = 400)
        => !Main.dedServ && float.IsFinite(at.X) && float.IsFinite(at.Y)
        && at.X >= Main.screenPosition.X - margin && at.X <= Main.screenPosition.X + Main.screenWidth + margin
        && at.Y >= Main.screenPosition.Y - margin && at.Y <= Main.screenPosition.Y + Main.screenHeight + margin;

    internal static Texture2D Texture(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D texture))
            textures[name] = texture = ModContent.Request<Texture2D>(Assets + name).Value;
        return texture;
    }
    internal static Texture2D Material(string name)
    {
        if (!textures.TryGetValue("material/" + name, out Texture2D texture))
            textures["material/" + name] = texture = CoreCasterPalette.Material(
                ModContent.Request<Texture2D>(CoreCasterArt.Assets + name).Value, 1);
        return texture;
    }

    internal static void Sprite(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation = 0)
    {
        if (!Visible(center) || !float.IsFinite(size.X) || !float.IsFinite(size.Y)
            || size.X <= 0 || size.Y <= 0 || size.X > 640 || size.Y > 640 || !float.IsFinite(rotation)) return;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }

    internal static void Line(Vector2 from, Vector2 to, Color tint, float width = 1)
    {
        Vector2 delta = to - from;
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() < .01f
            || delta.LengthSquared() > 700 * 700) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            tint, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), Math.Clamp(width, .5f, 12)), SpriteEffects.None);
    }

    internal static void Glow(Vector2 center, Vector2 size, Color color, float opacity)
        => Sprite(TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value, center, size, color * Math.Clamp(opacity, 0, 1));

    internal static void DrawSigil(Vector2 center, float radius, float opacity, int spokes = 6)
    {
        if (!Visible(center) || !float.IsFinite(radius) || !float.IsFinite(opacity) || opacity <= 0) return;
        radius = Math.Clamp(radius, 8, 240);
        opacity = Math.Clamp(opacity, 0, .9f);
        spokes = Math.Clamp(spokes, 3, 12);
        float clock = Main.GlobalTimeWrappedHourly;
        // The original elliptical ice ring establishes the ground plane. A broken
        // inner seal and faceted spokes rotate slowly inside that fixed silhouette.
        Sprite(Texture("ICRing"), center, new Vector2(radius * 2.2f, radius * .64f), Color.White * (opacity * .65f));
        for (int i = 0; i < 48; i++)
        {
            if (i % 8 == 0) continue;
            float a = i * MathHelper.TwoPi / 48 + clock * .14f;
            Vector2 from = center + a.ToRotationVector2() * new Vector2(radius, radius * .3f);
            Vector2 to = center + (a + MathHelper.TwoPi / 48).ToRotationVector2() * new Vector2(radius, radius * .3f);
            Line(from, to, Ice * (opacity * .5f), 1);
        }
        for (int i = 0; i < spokes; i++)
        {
            float a = i * MathHelper.TwoPi / spokes - clock * .11f;
            Vector2 axis = a.ToRotationVector2();
            Vector2 at = center + axis * new Vector2(radius * .72f, radius * .22f);
            Line(center + axis * new Vector2(radius * .5f, radius * .15f), at, Pale * (opacity * .5f));
            Sprite(Texture("FNIceCrystal"), at, new Vector2(6, 10), Color.White * opacity, a + MathHelper.PiOver2);
        }
    }

    internal static void DrawBoss(NPC npc, bool airborne, float attackProgress, int pattern)
    {
        if (!Visible(npc.Center) || npc.IsABestiaryIconDummy || npc.Opacity <= .02f) return;
        float opacity = npc.Opacity;
        float clock = Main.GlobalTimeWrappedHourly;
        float charge = float.IsFinite(attackProgress) ? Math.Clamp(attackProgress, 0, 1) : 0;
        Vector2 feet = npc.Bottom + new Vector2(0, airborne ? 13 : 3);
        if (airborne)
        {
            // Two offset, textured wind veils have independent speeds and depth.
            // Their low coverage leaves the actual boss and incoming shots readable.
            for (int i = 0; i < 3; i++)
            {
                float angle = clock * (.4f + i * .13f) * (i == 1 ? -1 : 1) + i * 1.9f;
                Vector2 at = feet + new Vector2(MathF.Sin(clock * 1.4f + i) * 10, 3 + i * 8);
                Sprite(Material("WindCrescent"), at, new Vector2(102 + i * 21, 31 + i * 6),
                    Color.White * (opacity * (.2f - i * .025f)), angle * .13f);
            }
            Glow(npc.Center + new Vector2(0, 8), new Vector2(94, 116), Ice, .09f * opacity);
            DrawSigil(feet, 52 + charge * 12, opacity * (.4f + charge * .25f));
            for (int i = 0; i < 6; i++)
            {
                float a = clock * .42f + i * MathHelper.TwoPi / 6;
                Vector2 at = npc.Center + a.ToRotationVector2() * new Vector2(47 + charge * 13, 20 + charge * 6);
                at.Y += MathF.Sin(clock * 1.7f + i) * 4;
                Sprite(Texture("FNIceCrystal"), at, new Vector2(8, 13), Color.White * (opacity * .7f), a * .2f);
            }
        }
        else if (charge > .03f)
            DrawSigil(feet, 35 + charge * 22, charge * opacity * .65f, 4 + Math.Abs(pattern % 4));

        // A compact glow at the hand resolves the start of each cast without
        // washing out her sprite or reproducing the large revival shader.
        if (charge > .1f)
        {
            Vector2 hand = npc.Center + new Vector2(npc.direction * 17, -9);
            Glow(hand, new Vector2(22 + charge * 19), Ice, charge * opacity * .22f);
            Sprite(Material("Crystal"), hand, new Vector2(8 + charge * 6, 15 + charge * 9),
                Color.White * (charge * opacity * .8f), clock * .5f);
        }
    }

    internal static void Cast(Vector2 center, int kind, float strength = 1)
    {
        if (!Visible(center) || !float.IsFinite(strength) || ParticleManager.activeParticles.Count >= 820) return;
        strength = Math.Clamp(strength, .25f, 2.5f);
        int count = Math.Clamp((int)((kind == 2 ? 22 : kind == 3 ? 6 : 12) * strength), 4, 44);
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 860; i++)
        {
            float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-.16f, .16f);
            Vector2 outward = angle.ToRotationVector2();
            int style = i % 5 == 0 ? 2 : i % 3 == 0 ? 1 : 0;
            new FrostNovaFrostMote(center + outward * Main.rand.NextFloat(2, 12),
                outward * Main.rand.NextFloat(1.4f, 4.4f) * MathF.Sqrt(strength), style,
                Main.rand.Next(25, 46), Main.rand.NextFloat(.75f, 1.4f) * MathF.Sqrt(strength), kind == 1).Spawn();
            if (i % 6 == 0)
                ChargedOperatorVfx.Sparkle(center + outward * 8, outward * 2.4f,
                    kind == 1 ? new Color(130, 153, 217) : Pale, angle, .45f * MathF.Sqrt(strength));
        }
    }

    internal static void DrawShard(Projectile projectile, float opacity)
    {
        Vector2 direction = projectile.velocity.SafeNormalize(-Vector2.UnitY);
        float angle = direction.ToRotation() + MathHelper.PiOver2;
        Glow(projectile.Center, new Vector2(32, 23), Ice, opacity * .16f);
        // Original ice sprite is the solid, dangerous part, with a source-material
        // lance behind it and a narrow pale fracture along its leading edge.
        Sprite(Material("EnergyLance"), projectile.Center - direction * 7,
            new Vector2(15, 44), Color.White * (opacity * .5f), angle);
        Sprite(Texture("FNIceCrystal"), projectile.Center, new Vector2(13, 22), Color.White * opacity, angle);
        Sprite(Material("EnergyLance"), projectile.Center + direction * 2,
            new Vector2(3, 18), Color.White * (opacity * .7f), angle);
    }

    internal static void DrawShardTrail(Projectile projectile, float opacity)
    {
        int count = 1;
        path[0] = projectile.Center + projectile.velocity.SafeNormalize(-Vector2.UnitY) * 9;
        distance[0] = 0;
        float length = 0;
        const float limit = 155;
        foreach (Vector2 old in projectile.oldPos)
        {
            if (old == Vector2.Zero || count >= path.Length) break;
            Vector2 next = old + projectile.Size * .5f;
            float step = Vector2.Distance(path[count - 1], next);
            if (!float.IsFinite(step) || step > 80) break;
            if (step < .4f) continue;
            if (length + step > limit) { next = Vector2.Lerp(path[count - 1], next, (limit - length) / step); step = limit - length; }
            path[count] = next;
            distance[count++] = length += step;
            if (length >= limit) break;
        }
        if (count < 3 || length < 4) return;
        for (int i = 0; i < count; i++)
        {
            float t = distance[i] / Math.Max(1, length);
            Vector2 tangent = (path[Math.Max(0, i - 1)] - path[Math.Min(count - 1, i + 1)]).SafeNormalize(Vector2.UnitY);
            Vector2 normal = new(-tangent.Y, tangent.X);
            float width = 7 * MathF.Pow(Math.Max(0, MathF.Sin(t * MathHelper.Pi)), .65f) * MathF.Sqrt(1 - t);
            Color tint = Color.White * opacity;
            mesh[i * 2] = new(new Vector3(path[i] - normal * width - Main.screenPosition, 0), tint, new Vector2(t, 0));
            mesh[i * 2 + 1] = new(new Vector3(path[i] + normal * width - Main.screenPosition, 0), tint, new Vector2(t, 1));
        }
        trail ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CoreCasterEnergyTrail").Value;
        flow ??= ModContent.Request<Texture2D>(CoreCasterArt.Assets + "EnergyFlow").Value;
        var device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            trail.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix
                * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
            trail.Parameters["FlowTexture"].SetValue(flow);
            trail.Parameters["Outer"].SetValue(Ice.ToVector3());
            trail.Parameters["Inner"].SetValue(Pale.ToVector3());
            trail.Parameters["Shadow"].SetValue(new Vector3(.025f, .08f, .16f));
            trail.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly + projectile.identity * .137f);
            trail.Parameters["Style"].SetValue(1f);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            trail.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, mesh, 0, count * 2 - 2);
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void Clear() { trail = null; flow = null; textures.Clear(); }
}

// Client-only facets, slowing ice streaks and low-coverage vapor. Every particle
// has a finite life and shares the established mod particle budget.
public sealed class FrostNovaFrostMote : Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/CoreCasters/EnergyLance";
    private int style;
    private float size;
    private float spin;
    private bool black;

    public FrostNovaFrostMote(Vector2 position, Vector2 velocity, int style, int life, float size, bool black)
    {
        Position = position; Velocity = velocity; this.style = style; this.size = size; this.black = black;
        Lifetime = Math.Clamp(life, 12, 60); Direction = 1;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi); spin = Main.rand.NextFloat(-.07f, .07f);
    }

    public override void Update()
    {
        Velocity *= style == 2 ? .975f : .947f;
        Velocity.Y -= style == 2 ? .018f : -.008f;
        Rotation += spin;
    }

    public override void Draw()
    {
        float t = LifetimeRatio;
        float opacity = Math.Min(1, t * 7) * MathF.Pow(1 - t, 1.6f);
        Color tint = black ? new Color(94, 124, 179) : Color.White;
        if (style == 2)
            FrostNovaVisuals.Sprite(FrostNovaVisuals.Material("Veil"), Position,
                new Vector2(32, 21) * size * (1 + t * .6f), tint * (opacity * .15f), Rotation);
        else if (style == 1)
            FrostNovaVisuals.Sprite(FrostNovaVisuals.Texture("FNIceCrystal"),
                Position, new Vector2(4, 9) * size * (1 - t * .4f), tint * (opacity * .75f), Rotation);
        else
            FrostNovaVisuals.Sprite(FrostNovaVisuals.Material("EnergyLance"), Position,
                new Vector2(2.5f, 13 + Velocity.Length() * 2) * size * (1 - t * .5f),
                tint * (opacity * .75f), Velocity.ToRotation() + MathHelper.PiOver2);
    }
}

public sealed class FrostNovaVisualSystem : ModSystem
{
    public override void Unload() => FrostNovaVisuals.Clear();
}
