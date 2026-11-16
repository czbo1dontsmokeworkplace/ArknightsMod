using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// All dimensions in this renderer are final world pixels. Existing FrostNova art
// stays the silhouette; added layers use FrostNova's original ice/wind artwork.
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
    private static readonly Dictionary<Texture2D, Texture2D> outlines = new();

    internal static bool SecondPhase(Projectile projectile)
    {
        var owner = projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>();
        return FrostNovaOwnedProjectile.Resolve(owner.Host, owner.Token, out FrostNova boss) && boss.AirbornePhase;
    }
    // One cached silhouette pass, using original alpha. Bright rims do not fill
    // the black crystal or turn it into several displaced copies of the sprite.
    internal static void Outline(Texture2D body, Vector2 position, Rectangle? source, float rotation, Vector2 origin, Vector2 scale, float opacity, int frames = 1)
    {
        if (Main.dedServ || opacity <= 0) return;
        if (!outlines.TryGetValue(body, out Texture2D edge)) {
            int w = body.Width, h = body.Height / frames, ew = w + 2, eh = h + 2;
            var pixels = new Color[body.Width * body.Height]; body.GetData(pixels);
            var mask = new Color[ew * eh * frames];
            for (int frame = 0; frame < frames; frame++)
                for (int y = -1; y <= h; y++) for (int x = -1; x <= w; x++) {
                    int Alpha(int px, int py) => px >= 0 && px < w && py >= 0 && py < h ? pixels[(frame * h + py) * w + px].A : 0;
                    int a = Math.Max(Math.Max(Alpha(x - 1, y), Alpha(x + 1, y)), Math.Max(Alpha(x, y - 1), Alpha(x, y + 1)));
                    byte alpha = (byte)Math.Max(0, a - Alpha(x, y));
                    mask[((frame * eh) + y + 1) * ew + x + 1] = new Color(alpha, alpha, alpha, alpha);
                }
            edge = new Texture2D(Main.spriteBatch.GraphicsDevice, ew, eh * frames); edge.SetData(mask); outlines.Add(body, edge);
        }
        Rectangle src = source ?? new Rectangle(0, 0, body.Width, body.Height / frames);
        int index = src.Y / Math.Max(1, body.Height / frames), height = edge.Height / frames;
        Main.EntitySpriteDraw(edge, position - Main.screenPosition, new Rectangle(0, index * height, edge.Width, height),
            Pale * opacity, rotation, origin + Vector2.One, scale, SpriteEffects.None);
    }

    internal static bool Visible(Vector2 at, float margin = 400)
        => !Main.dedServ && float.IsFinite(at.X) && float.IsFinite(at.Y)
        && at.X >= Main.screenPosition.X - margin && at.X <= Main.screenPosition.X + Main.screenWidth + margin
        && at.Y >= Main.screenPosition.Y - margin && at.Y <= Main.screenPosition.Y + Main.screenHeight + margin;

    internal static Texture2D Texture(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D texture))
            textures[name] = texture = ModContent.Request<Texture2D>(Assets + name, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        return texture;
    }
    internal static Texture2D Material(string name)
    {
        if (!textures.TryGetValue("material/" + name, out Texture2D texture))
            textures["material/" + name] = texture = ModContent.Request<Texture2D>(OriginalMaterialPath(name), ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        return texture;
    }
    internal static string OriginalMaterialPath(string name) => name switch {
        "WindCrescent" => Assets + "ICRing",
        "Crystal" or "EnergyLance" => Assets + "FNIceCrystal",
        "Veil" => Assets + "Aerosol/Aerosol01",
        "EnergyFlow" => "ArknightsMod/Common/VisualEffects/WindTrail",
        _ => Assets + "ICGlow"
    };

    internal static void Sprite(Texture2D texture, Vector2 center, Vector2 size, Color tint, float rotation = 0)
    {
        if (!Visible(center) || !float.IsFinite(size.X) || !float.IsFinite(size.Y)
            || size.X <= 0 || size.Y <= 0 || size.X > 640 || size.Y > 640 || !float.IsFinite(rotation)) return;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }

    internal static void Glow(Vector2 center, Vector2 size, Color color, float opacity)
        => Sprite(TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value, center, size, color * Math.Clamp(opacity, 0, 1));

    internal static void DrawSigil(Vector2 center, float radius, float opacity, int spokes = 6)
    {
        // Casting is now emitted as real mist/ice particles in AI, never drawn as rings.
    }

    internal static void DrawBoss(NPC npc, bool airborne, float attackProgress, int pattern)
    {
        if (!Visible(npc.Center) || npc.IsABestiaryIconDummy || npc.Opacity <= .02f) return;
        float opacity = npc.Opacity;
        float clock = Main.GlobalTimeWrappedHourly;
        float charge = float.IsFinite(attackProgress) ? Math.Clamp(attackProgress, 0, 1) : 0;
        Vector2 feet = npc.Center + new Vector2(0, 29 + (airborne ? 13 : 3));
        // Both phases chant on the ground; the halo is drawn around the body separately.
        if (charge > .03f)
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
        if (!Visible(center) || !float.IsFinite(strength) || ParticleManager.activeParticles.Count >= 1900) return;
        strength = Math.Clamp(strength, .25f, 2.5f);
        if (kind != 3) FrostNovaColdMistSystem.Begin(center);
        int count = Math.Clamp((int)((kind == 2 ? 22 : kind == 3 ? 6 : 12) * strength), 4, 44);
        for (int i = 0; i < Math.Min(10, count / 2); i++) {
            Vector2 axis = (i * MathHelper.TwoPi / Math.Max(1, count / 2)).ToRotationVector2();
            Dust d = Dust.NewDustPerfect(center, i % 2 == 0 ? 240 : 263, axis * (1.2f + strength),
                90, Color.White, .8f + strength * .3f); d.noGravity = true;
        }
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 1950; i++)
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

    internal static void Shatter(Vector2 center, Vector2 velocity, int count = 9)
    {
        if (!Visible(center)) return;
        count = Math.Clamp(count, 3, 28);
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 1950; i++) {
            Vector2 speed = Main.rand.NextVector2Circular(3.5f, 3.5f) - Vector2.UnitY * 1.5f + velocity * .06f;
            new FrostNovaBlackIceShardParticle(center, speed, Main.rand.Next(18, 33), Main.rand.NextFloat(3, 7)).Spawn();
            if (i % 3 == 0) {
                Dust dust = Dust.NewDustPerfect(center, DustID.IceTorch, speed, 100, Pale, .8f);
                dust.noGravity = true;
            }
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
        Texture2D crystal = Texture("BlackIcePrism");
        int frame = projectile.ai[2] > 0 ? Math.Clamp(4 - (int)(projectile.ai[2] / 15), 0, 3) : 4;
        Rectangle source = crystal.Frame(1, 5, 0, frame);
        Vector2 size = new(30, 60);
        Outline(crystal, projectile.Center, source, angle, source.Size() * .5f, size / source.Size(), opacity, 5);
        Main.EntitySpriteDraw(crystal, projectile.Center - Main.screenPosition, source, Color.White * opacity,
            angle, source.Size() * .5f, size / source.Size(), SpriteEffects.None);
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
        trail ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CoreCasterEnergyTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        flow ??= Material("EnergyFlow");
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

    internal static void Clear() {
        FrostNovaProjectileArt.ExternalTextures.Clear();
        trail = null; flow = null; textures.Clear();
        foreach (Texture2D texture in outlines.Values) texture.Dispose();
        outlines.Clear();
    }
    internal static void CrystalWake(Vector2 center, float rotation, int age)
    {
        if (!Visible(center)) return;
        for (int i = 0; i < 2; i++) {
            Vector2 offset = new Vector2((i == 0 ? 1 : -1) * 7 * MathF.Sin(age * MathHelper.Pi / 18), -18).RotatedBy(rotation);
            Dust dust = Dust.NewDustPerfect(center + offset, i == 0 ? 240 : 263, Vector2.Zero, 60, Color.White, 1.1f);
            dust.noGravity = true;
        }
    }
}

// Client-only facets, slowing ice streaks and low-coverage vapor. Every particle
// has a finite life and shares the established mod particle budget.
public sealed class FrostNovaFrostMote : Particle
{
    public override string TexturePath => FrostNovaVisuals.Assets + "FNIceCrystal";
    public override BlendState DrawBlendState => style == 2 ? BlendState.Additive : BlendState.AlphaBlend;
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
