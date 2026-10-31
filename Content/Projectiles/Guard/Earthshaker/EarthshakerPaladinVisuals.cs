using System;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;

internal static class EarthshakerPaladinVisuals
{
    // The 1.4.5.8 particle counts, shapes, velocities and draw offsets are kept;
    // only the two particle colors and hammer tint use the operator palette.
    internal static void Flight(Projectile shot, EarthshakerKind kind)
    {
        if (Main.dedServ || shot.ai[1] < 30f) return;
        float spread = 16f * shot.scale;
        Vector2 normal = shot.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        for (int i = 0; i < 10; i++)
        {
            if (Main.rand.Next(2) == 0)
            {
                Dust dust = Mote(shot.position, shot.width, shot.height, false, kind, shot.velocity * .2f, 200, 1.2f);
                dust.velocity += shot.velocity * 1.76f * Main.rand.NextFloat();
                dust.velocity *= .2f;
                dust.noGravity = true;
                float radius = spread;
                if (Main.rand.Next(3) == 0) radius *= Main.rand.NextFloat();
                dust.position = shot.Center + normal * radius * (Main.rand.Next(2) * 2 - 1);
            }
            if (Main.rand.Next(3) == 0)
            {
                Dust dust = Mote(shot.position, shot.width, shot.height, true, kind, Vector2.Zero, 254, .3f);
                dust.velocity += shot.velocity * 1.76f * Main.rand.NextFloat();
                dust.velocity *= .5f;
                dust.noGravity = true;
                float radius = spread;
                if (Main.rand.Next(3) == 0) radius *= Main.rand.NextFloat();
                dust.position = shot.Center + normal * radius * (Main.rand.Next(2) * 2 - 1);
            }
        }
    }

    private static Dust Mote(Vector2 position, int width, int height, bool accent, EarthshakerKind kind,
        Vector2 velocity, int alpha, float scale)
    {
        // Spawn as vanilla first to preserve its atlas frame and initialization.
        Dust dust = Dust.NewDustDirect(position, width, height, accent ? 43 : 57,
            velocity.X, velocity.Y, alpha, accent ? EarthshakerRules.Accent(kind) : EarthshakerRules.Tint(kind), scale);
        if (dust.active)
        {
            dust.type = accent ? ModContent.DustType<EarthshakerPaladinGlow>() : ModContent.DustType<EarthshakerPaladinSpark>();
            dust.noLightEmittence = true;
            EarthshakerPaladinDustLighting.HasMotes = true;
        }
        return dust;
    }

    internal static void Shockwave(Projectile shot, Vector2 position, EarthshakerKind kind)
    {
        if (Main.dedServ) return;
        Vector2 center = position + shot.Size * .5f;
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sounds/EarthshakerPaladinImpact"), center);
        for (int i = 0; i < 50; i++)
        {
            if (Main.rand.Next(2) == 0)
            {
                Dust dust = Mote(position, shot.width, shot.height, false, kind, Vector2.Zero, 200, 1.8f);
                dust.velocity = (dust.position - center).SafeNormalize(Vector2.Zero) * 9.5f;
                dust.noGravity = true;
                dust.velocity *= Main.rand.NextFloat();
            }
            if (Main.rand.Next(3) == 0)
            {
                Dust dust = Mote(position, shot.width, shot.height, true, kind, Vector2.Zero, 254, 1.3f);
                dust.velocity = (dust.position - center).SafeNormalize(Vector2.Zero) * 9.5f;
                dust.noGravity = true;
                dust.velocity *= Main.rand.NextFloat();
            }
        }

        Point origin = position.ToTileCoordinates();
        for (int x = origin.X - 3; x <= origin.X + 3; x++)
        for (int y = origin.Y - 3; y <= origin.Y + 3; y++)
        {
            if (!WorldGen.InWorld(x, y)) continue;
            Tile tile = Framing.GetTileSafely(x, y);
            if (!tile.HasTile) continue;
            // Dust-only helpers: the original wall burst never mines the tiles.
            int count = WorldGen.KillTile_GetTileDustAmount(true, tile, x, y);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Main.dust[WorldGen.KillTile_MakeTileDust(x, y, tile)];
                dust.velocity.Y -= 9f;
                dust.velocity.Y *= Main.rand.NextFloat();
                dust.velocity.Y *= .75f;
                dust.scale += .12f;
            }
            for (int i = 0; i < count - 1; i++)
            {
                Dust dust = Main.dust[WorldGen.KillTile_MakeTileDust(x, y, tile)];
                dust.velocity.Y -= 5f;
                dust.velocity.Y *= Main.rand.NextFloat();
                dust.velocity.Y *= .75f;
            }
            if (count > 0 && Main.rand.Next(2) != 0)
            {
                // Keep vanilla's integer division here (center vs outer column).
                float edge = Math.Abs(origin.X - x) / 3;
                Gore gore = Gore.NewGoreDirect(shot.GetSource_FromThis(), center, Vector2.Zero,
                    61 + Main.rand.Next(3), .4f + edge * .5f);
                gore.velocity.Y -= 2.1f + edge * 4f;
                gore.velocity.Y *= Main.rand.NextFloat();
                gore.position = new Vector2(x * 16 + 20, y * 16);
            }
        }
    }

    internal static void Draw(Projectile shot, EarthshakerKind kind, Color light)
    {
        Texture2D texture = TextureAssets.Projectile[ProjectileID.PaladinsHammerFriendly].Value;
        Vector2 origin = new(texture.Width * .5f, shot.height * .5f);
        Vector2 position = shot.Center - Main.screenPosition + new Vector2(0f, shot.gfxOffY);
        Color tint = EarthshakerRules.Tint(kind);
        Color source = shot.ai[0] == 1f ? light : new Color(250, 250, 250, 50);
        Color color = new(source.R * tint.R / 255, source.G * tint.G / 255, source.B * tint.B / 255, source.A);
        SpriteEffects effects = shot.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 offset = (shot.rotation - MathHelper.PiOver4).ToRotationVector2() * 16f * shot.scale;
        for (int i = 9; i >= 0; i -= 2)
        {
            Color trail = color * ((9f - i) / 9f);
            if (shot.ai[0] == 1f) trail *= .5f;
            // Preserve the source's ai[0] test: outbound afterimages are hidden.
            if (shot.ai[0] == 0f) trail *= 0f;
            float rotation = shot.rotation - shot.direction * (MathHelper.Pi / 10f) * (i + 1);
            Vector2 trailOffset = (rotation - MathHelper.PiOver4).ToRotationVector2() * 4f * shot.scale;
            Main.EntitySpriteDraw(texture, position + trailOffset - shot.velocity * i * .45f, null,
                trail * .5f, rotation, origin, shot.scale, effects);
            Main.EntitySpriteDraw(texture, position - trailOffset, null,
                trail * .05f, rotation + MathHelper.Pi, origin, shot.scale * .8f, effects);
        }
        Main.EntitySpriteDraw(texture, position - offset, null, color * .1f,
            shot.rotation + MathHelper.Pi, origin, shot.scale * .8f, effects);
        Main.EntitySpriteDraw(texture, position + offset, null, color,
            shot.rotation, origin, shot.scale, effects);
    }
}

// Use the original dust atlas and update types, including their grow/fade rules.
// Custom alpha is required: vanilla dust 57 ignores an assigned dust.color.
public abstract class EarthshakerPaladinDust : ModDust
{
    protected abstract int VanillaType { get; }
    public override string Texture => "Terraria/Images/Dust";
    public override void SetStaticDefaults() => UpdateType = VanillaType;

    public override Color? GetAlpha(Dust dust, Color lightColor)
    {
        float alpha = (255f - dust.alpha) / 255f;
        float brightness = VanillaType == 57 ? (alpha + 3f) / 4f : (alpha + 9f) / 10f;
        return new Color((int)(lightColor.R * dust.color.R / 255f * brightness),
            (int)(lightColor.G * dust.color.G / 255f * brightness),
            (int)(lightColor.B * dust.color.B / 255f * brightness), Math.Max(0, lightColor.A - dust.alpha));
    }

}

public sealed class EarthshakerPaladinDustLighting : ModSystem
{
    internal static bool HasMotes;
    public override void Unload() => HasMotes = false;
    public override void PostUpdateDusts()
    {
        if (Main.dedServ || !HasMotes) return;
        HasMotes = false;
        int sparkType = ModContent.DustType<EarthshakerPaladinSpark>();
        int glowType = ModContent.DustType<EarthshakerPaladinGlow>();
        foreach (Dust dust in Main.dust)
        {
            if (!dust.active) continue;
            Vector3 color;
            if (dust.type == sparkType) color = dust.color.ToVector3() * Math.Min(1f, dust.scale * .8f);
            else if (dust.type == glowType)
                color = Lighting.GetColor(dust.position.ToTileCoordinates()).ToVector3()
                    * (255f / 270f) * dust.color.ToVector3() * (dust.scale * 1.07f);
            else continue;
            HasMotes = true;
            Lighting.AddLight(dust.position, color);
        }
    }
}

public sealed class EarthshakerPaladinSpark : EarthshakerPaladinDust
{
    protected override int VanillaType => 57;
}

public sealed class EarthshakerPaladinGlow : EarthshakerPaladinDust
{
    protected override int VanillaType => 43;
}
