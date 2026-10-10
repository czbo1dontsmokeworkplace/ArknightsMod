using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.Sniper.Fiammetta;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

internal static class MagallanVisuals
{
    private static readonly Dictionary<Texture2D, Texture2D> outlines = new();

    // A client-side alpha mask keeps the outline truly blue/green/amber without tinting the supplied artwork.
    public static Texture2D OutlineMask(Texture2D source)
    {
        if (outlines.TryGetValue(source, out Texture2D mask)) return mask;
        Color[] pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = pixels[i].A;
            pixels[i] = new Color(a, a, a, a);
        }
        mask = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height);
        mask.SetData(pixels);
        outlines[source] = mask;
        return mask;
    }

    internal static void Unload()
    {
        Texture2D[] cached = new Texture2D[outlines.Count];
        outlines.Values.CopyTo(cached, 0);
        outlines.Clear();
        Main.QueueMainThreadAction(() => { foreach (Texture2D mask in cached) mask.Dispose(); });
    }
    public static Color ModuleColor(int module) => module switch
    {
        0 => new Color(110, 218, 255),
        1 => new Color(125, 255, 170),
        _ => new Color(255, 178, 65)
    };

    internal const string SmokePath = "ArknightsMod/Assets/GrayScaleTexture/Smoke2";
    private const string GlowPath = "ArknightsMod/Common/Particle/DefaultParticle";
    private const string FlamePath = "ArknightsMod/Content/Textures/muzzle_02";
    private const string ExplosionPath = "ArknightsMod/Content/Projectiles/Sniper/Fiammetta/Assets/FiammettaExplosionBloom";

    internal static bool CanEmit(Vector2 center) => !Main.dedServ && ParticleManager.activeParticles.Count < 1000
        && Vector2.DistanceSquared(center, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f) < 1800f * 1800f;

    // Pixel dimensions are normalized by the actual texture dimensions. No decorative line geometry.
    private static void Stamp(string path, Vector2 center, Vector2 size, Color color, float rotation = 0f)
    {
        if (Main.dedServ || !float.IsFinite(size.X) || !float.IsFinite(size.Y)
            || size.X <= 0 || size.Y <= 0 || size.X > 520f || size.Y > 520f) return;
        float margin = 350f;
        if (center.X < Main.screenPosition.X - margin || center.X > Main.screenPosition.X + Main.screenWidth + margin
            || center.Y < Main.screenPosition.Y - margin || center.Y > Main.screenPosition.Y + Main.screenHeight + margin) return;
        Texture2D texture = ModContent.Request<Texture2D>(path).Value;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, rotation,
            texture.Size() * 0.5f, size / texture.Size(), SpriteEffects.None);
    }

    private static Color Light(Color color, float strength) => new Color(color.R, color.G, color.B, 0)
        * MathHelper.Clamp(strength, 0f, 1f);

    public static void Glow(Vector2 center, Color color, float radius, float strength)
    {
        Stamp(GlowPath, center, new Vector2(radius * 2f), Light(color, strength));
    }

    public static void Puff(Vector2 center, Vector2 velocity, Color tint, float diameter, int lifetime = 40,
        float opacity = 0.55f, float lift = -0.015f)
    {
        if (!CanEmit(center)) return;
        new MagallanHeavySmokeParticle(center, velocity, tint, diameter, lifetime, opacity, lift).Spawn();
    }

    public static void SwitchBurst(Vector2 center, int mode)
    {
        if (!CanEmit(center)) return;
        Color tint = Color.Lerp(ModuleColor(mode), new Color(195, 210, 219), 0.7f);
        for (int i = 0; i < 5; i++)
            Puff(center + Main.rand.NextVector2Circular(9f, 7f),
                Main.rand.NextVector2Circular(1.3f, 0.7f) + Vector2.UnitY * 0.6f,
                tint, Main.rand.NextFloat(22f, 34f), 26, 0.35f, 0.025f);
    }

    public static void SparkBurst(Vector2 center, Color color, int count, float speed)
    {
        if (!CanEmit(center)) return;
        for (int i = 0; i < Math.Min(count, 8); i++)
            new DefaultParticle(center, Main.rand.NextVector2Circular(speed, speed), Main.rand.Next(12, 23),
                Main.rand.NextFloat(0.035f, 0.065f), color * 0.65f, true)
                { Deformation = Vector2.One }.Spawn();
    }

    public static void ColdMist(Vector2 center, float radius, bool boosted)
    {
        if (!CanEmit(center)) return;
        for (int i = 0; i < 3; i++)
        {
            Vector2 offset = Main.rand.NextVector2Circular(radius * 0.85f, radius * 0.85f);
            Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.8f, 1.8f) + Vector2.UnitY * 0.45f;
            Puff(center + offset, velocity, i == 0 ? new Color(115, 169, 195) : new Color(192, 222, 232),
                Main.rand.NextFloat(64f, boosted ? 110f : 92f), Main.rand.Next(34, 48), 0.48f, 0.018f);
        }
    }

    public static void LaserVapor(Vector2 start, Vector2 end, bool boosted)
    {
        if (!CanEmit(start)) return;
        Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
        Puff(end, -direction * 0.4f + Main.rand.NextVector2Circular(0.5f, 0.6f),
            new Color(145, 198, 169), boosted ? 30f : 22f, 24, 0.32f);
        if (boosted)
            Puff(Vector2.Lerp(start, end, Main.rand.NextFloat(0.15f, 0.8f)), direction * 0.3f,
                new Color(114, 169, 146), 18f, 20, 0.18f);
    }

    public static void Beam(Vector2 start, Vector2 end, float opacity, bool boosted)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (!float.IsFinite(length) || length < 1f || length > MagallanLaser.MaxLength + 1f) return;
        float angle = delta.ToRotation();
        // This short beam uses Evolution's painted body once at its full length. No repeated strips or joins.
        Texture2D body = OutlineMask(ModContent.Request<Texture2D>(
            "ArknightsMod/Content/NPCs/Enemy/Evolution/Assets/EvolutionBeamBody").Value);
        Rectangle source = new(0, 258, 2172, 204);
        Main.EntitySpriteDraw(body, start - Main.screenPosition, source,
            new Color(66, 175, 119) * (opacity * 0.65f), angle,
            new Vector2(0f, source.Height * 0.5f), new Vector2(length / source.Width, 24f / source.Height), SpriteEffects.None);
        Main.EntitySpriteDraw(body, start - Main.screenPosition, source,
            new Color(204, 255, 215) * (opacity * 0.95f), angle,
            new Vector2(0f, source.Height * 0.5f), new Vector2(length / source.Width, 8f / source.Height), SpriteEffects.None);
        Glow(start, ModuleColor(1), 22f, opacity * 0.65f);
        Glow(end, ModuleColor(1), 19f, opacity * 0.55f);
    }

    public static void Shell(Vector2 center, Vector2 velocity)
    {
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
        Vector2 exhaust = center - direction * 12f;
        Stamp(FlamePath, exhaust - direction * 5f, new Vector2(13f, 31f),
            Light(new Color(255, 117, 33), 0.85f), direction.ToRotation() - MathHelper.PiOver2);
        Stamp(FlamePath, exhaust, new Vector2(7f, 19f),
            Light(new Color(255, 233, 167), 0.95f), direction.ToRotation() - MathHelper.PiOver2);
        Texture2D rocket = TextureAssets.Projectile[ProjectileID.RocketIV].Value;
        Main.EntitySpriteDraw(rocket, center - Main.screenPosition, null, Color.White,
            direction.ToRotation() + MathHelper.PiOver2, rocket.Size() * 0.5f, 1.3f, SpriteEffects.None);
        Glow(exhaust, new Color(255, 117, 38), 15f, 0.45f);
    }

    public static void MissileTrail(Vector2 center, Vector2 velocity, int age)
    {
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
        Vector2 exhaust = center - direction * 15f;
        if (age % 2 == 0)
            Puff(exhaust - direction * Main.rand.NextFloat(4f, 13f),
                -direction * Main.rand.NextFloat(1.5f, 3.3f) + Main.rand.NextVector2Circular(0.7f, 0.7f),
                age % 4 == 0 ? new Color(115, 106, 97) : new Color(78, 83, 87),
                Main.rand.NextFloat(21f, 32f), Main.rand.Next(25, 35), 0.48f);
        if (CanEmit(exhaust))
            new DefaultParticle(exhaust, -direction * Main.rand.NextFloat(2f, 5f)
                + Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(10, 17),
                Main.rand.NextFloat(0.11f, 0.2f), age % 3 == 0
                    ? new Color(255, 238, 176) : new Color(255, 111, 36), true).Spawn();
    }

    public static void ExplosionSmoke(Vector2 center, float radius)
    {
        if (!CanEmit(center)) return;
        for (int i = 0; i < 18; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(4.8f, 3.5f) - Vector2.UnitY * 0.4f;
            Puff(center + velocity * 3f, velocity, i % 3 == 0 ? new Color(126, 103, 79) : new Color(76, 79, 83),
                radius * Main.rand.NextFloat(0.8f, 1.4f), Main.rand.Next(35, 49), 0.58f, -0.025f);
        }
        for (int i = 0; i < 22; i++)
        {
            Vector2 direction = Main.rand.NextVector2Unit();
            new DefaultParticle(center + direction * Main.rand.NextFloat(2f, radius * 0.35f),
                direction * Main.rand.NextFloat(3f, 9f), Main.rand.Next(15, 27),
                Main.rand.NextFloat(0.18f, 0.36f), i % 3 == 0
                    ? new Color(255, 237, 173) : new Color(255, 102, 31), true).Spawn();
        }
    }

    public static void Explosion(Vector2 center, float radius, float progress)
    {
        FiammettaVisuals.DrawExplosion(center, radius * 2.7f, progress, 0);
    }
}

// Own ParticleManager and textures, with heavy-smoke style expansion, rolling shading and finite decay.
public sealed class MagallanHeavySmokeParticle : Particle
{
    public override string TexturePath => MagallanVisuals.SmokePath;
    private readonly float diameter;
    private readonly float opacity;
    private readonly float lift;
    private readonly float spin;
    private readonly int variant;
    private readonly Color tint;

    public MagallanHeavySmokeParticle(Vector2 position, Vector2 velocity, Color tint, float diameter,
        int lifetime, float opacity, float lift)
    {
        Position = position;
        Velocity = velocity;
        this.tint = tint;
        this.diameter = MathHelper.Clamp(diameter, 8f, 120f);
        this.opacity = MathHelper.Clamp(opacity, 0f, 0.65f);
        this.lift = lift;
        Lifetime = Math.Clamp(lifetime, 12, 60);
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        spin = Main.rand.NextFloat(-0.017f, 0.017f);
        variant = Main.rand.Next(1, 5);
    }

    public override void Update()
    {
        Velocity *= 0.92f;
        Velocity.Y += lift;
        Rotation += spin;
    }

    public override void Draw()
    {
        float t = MathHelper.Clamp(LifetimeRatio, 0f, 1f);
        float fade = MathHelper.Clamp(t * 6f, 0f, 1f) * MathF.Pow(1f - t, 1.25f) * opacity;
        float size = diameter * (0.65f + 0.65f * MathF.Sin(t * MathHelper.Pi * 0.8f));
        Texture2D shape = ModContent.Request<Texture2D>($"ArknightsMod/Assets/GrayScaleTexture/Smoke{variant}").Value;
        Vector2 scale = new Vector2(size, size * (0.76f + 0.08f * MathF.Sin(t * 4f))) / shape.Size();
        Color shadow = Color.Lerp(tint, new Color(42, 60, 72), 0.48f);
        Main.EntitySpriteDraw(shape, Position - Main.screenPosition, null, shadow * fade,
            Rotation, shape.Size() * 0.5f, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(Texture, Position - Main.screenPosition + new Vector2(-size * 0.05f, -size * 0.08f),
            null, tint * (fade * 0.7f), -Rotation * 0.65f,
            Texture.Size() * 0.5f, new Vector2(size * 0.86f, size * 0.68f) / Texture.Size(), SpriteEffects.None);
    }
}

public sealed class MagallanVisualSystem : ModSystem
{
    public override void Unload()
    {
        if (!Main.dedServ) MagallanVisuals.Unload();
    }
}
