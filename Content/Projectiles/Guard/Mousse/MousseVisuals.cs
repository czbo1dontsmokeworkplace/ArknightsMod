using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

// AugerHoldout / AugerSlash visual timing and geometry reference, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
// Dust uses explicitly premultiplied local masks. Blade/slash use straight masks + Additive.
internal static class MousseVisuals
{
    internal const float RangeMultiplier = 1.25f;
    internal static readonly Color Pale = new(255, 244, 172);
    internal static Color Light(Color color, float strength = 1f)
        => new Color(color.R, color.G, color.B, 0) * strength;

    private static void BeginLayer(BlendState blend)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, blend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }

    private static void EndGlow()
    {
        Main.spriteBatch.End();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }

    internal static void DrawEnergyColumn(Vector2 root, float angle, float power, Color tint, float scale)
    {
        float charge = MathHelper.Clamp(power / 1.5f, 0f, 1f);
        if (charge < .01f) return;
        Texture2D blade = MousseMaterials.GetStraight("MousseEnergyColumn");
        Rectangle bounds = MousseMaterials.GetVisibleBounds("MousseEnergyColumn");
        float rotation = angle + MathHelper.PiOver2;
        BeginLayer(BlendState.NonPremultiplied);
        try
        {
            Color body = new((byte)(tint.R * .82f), (byte)(tint.G * .67f),
                (byte)(tint.B * .5f), (byte)205);
            DrawPixels(blade, root, new Vector2(37f * charge, 145f * charge * charge) * scale,
                body, rotation, new Vector2(.5f, 1f), source: bounds);
        }
        finally { EndGlow(); }
        BeginLayer(BlendState.Additive);
        try
        {
            // Portrait Y is the long axis. Full charge reaches 135 * scale world pixels.
            DrawPixels(blade, root, new Vector2(27f * charge, 135f * charge * charge) * scale,
                new Color(tint.R, tint.G, tint.B, (byte)180), rotation,
                new Vector2(.5f, 1f), source: bounds);
            DrawPixels(blade, root, new Vector2(12f * charge, 132f * charge * charge) * scale,
                new Color(255, 255, 245, (byte)100), rotation,
                new Vector2(.5f, 1f), source: bounds);
            DrawPixels(MousseMaterials.GetStraight("GlowOrb"), root,
                new Vector2(22f * charge, 32f * charge * charge) * scale,
                new Color(tint.R, tint.G, tint.B, (byte)95), rotation, new Vector2(.5f));
        }
        finally { EndGlow(); }
    }

    internal static void DrawPixels(Texture2D texture, Vector2 world, Vector2 size, Color color,
        float rotation, Vector2 origin, SpriteEffects flip = SpriteEffects.None, Rectangle? source = null)
    {
        if (!float.IsFinite(size.X + size.Y) || size.X <= 0f || size.Y <= 0f) return;
        size = Vector2.Min(size, new Vector2(1000f));
        Vector2 sourceSize = source.HasValue ? new Vector2(source.Value.Width, source.Value.Height) : texture.Size();
        Main.EntitySpriteDraw(texture, world - Main.screenPosition, source, color,
            rotation, origin * sourceSize, size / sourceSize, flip);
    }

    internal static void DrawHeldBlade(Projectile p, Player owner, float power, Color lightColor,
        Color tint, bool physicalSword)
    {
        Vector2 hand = p.Center + new Vector2(0f, owner.gfxOffY);
        Texture2D weapon = TextureAssets.Projectile[p.type].Value;
        power = MathHelper.Clamp(power, 0f, 1.5f);
        float scale = p.scale * RangeMultiplier;
        Vector2 root = hand + p.rotation.ToRotationVector2() * (physicalSword ? 16f * p.scale : 20f);
        float rotation = p.rotation + MathHelper.PiOver2;
        DrawEnergyColumn(root, p.rotation, power, tint, scale);
        if (physicalSword)
        {
            // The existing sword points from its lower-left grip to its upper-right tip.
            Main.EntitySpriteDraw(weapon, hand - Main.screenPosition, null, lightColor,
                p.rotation + MathHelper.PiOver4, new Vector2(8f, weapon.Height - 8f),
                p.scale, SpriteEffects.None);
        }
        else
            Main.EntitySpriteDraw(weapon, hand - Main.screenPosition, null, lightColor,
                rotation, new Vector2(weapon.Width * .5f, weapon.Height - 2f), 1f, SpriteEffects.None);
    }

    internal static void DrawSlash(Projectile p, int age, Color tint)
    {
        float progress = MathHelper.Clamp(age / 25f, 0f, 1f);
        float stretch = 1f - (1f - progress) * (1f - progress);
        float envelope = MathF.Pow(Utils.GetLerpValue(25f, 5f, age, true), 5f);
        if (envelope <= 0f) return;
        float scale = p.ai[1] * RangeMultiplier;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 size = new Vector2(1f + stretch * .5f, 1.25f - stretch * 1.1f) * (156.8f * envelope * scale);
        SpriteEffects flip = (p.ai[0] == 1f) == (aim.X >= 0f) ? SpriteEffects.FlipVertically : SpriteEffects.None;
        BeginLayer(BlendState.NonPremultiplied);
        try
        {
            DrawPixels(MousseMaterials.GetStraight("MousseCrescent"), p.Center + aim * 105f * scale,
                size, new Color((byte)(tint.R * .82f), (byte)(tint.G * .75f),
                    (byte)(tint.B * .63f), (byte)(200f * envelope)),
                aim.ToRotation(), new Vector2(.85f, .5f), flip);
        }
        finally { EndGlow(); }
        BeginLayer(BlendState.Additive);
        try
        {
            DrawPixels(MousseMaterials.GetStraight("MousseCrescent"), p.Center + aim * 105f * scale,
                size * new Vector2(.9f, .86f), new Color(tint.R, tint.G, tint.B, (byte)(90f * envelope)),
                aim.ToRotation(), new Vector2(.85f, .5f), flip);
        }
        finally { EndGlow(); }
    }

    // P-GSS: Auger slash emission reference: Azafure, LLC.
    // https://github.com/CalamityTeam/CalamityModPublic
    // This uses only Terraria dust and this mod's own mask; no Calamity runtime types/assets.
    internal static void EmitAugerSlash(Projectile p, Color tint)
    {
        if (Main.dedServ || Main.gameMenu || p.owner < 0 || p.owner >= Main.maxPlayers
            || Vector2.DistanceSquared(p.Center, Main.LocalPlayer.Center) > 2400f * 2400f)
            return;

        const int dustCount = 30;
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        float weaponScale = p.ai[1] * RangeMultiplier;
        for (int i = 0; i < dustCount; i++)
        {
            float variance = Utils.GetLerpValue(0f, dustCount, i, true) * Main.rand.NextFloat(.9f, 1f);
            float angle = MathHelper.Lerp(-.7f, .7f, variance) * p.direction;
            float swingWeight = p.ai[0] == 1f ? 1f - variance : variance;
            float particleScale = .4f + swingWeight * 2f;
            float centerWeight = 1f - MathF.Abs(angle);
            float centerPower = MathF.Pow(centerWeight, 1.5f);
            Vector2 radial = aim.RotatedBy(angle)
                * (70f + 80f * centerPower * Main.rand.NextFloat(.85f, 1f));
            Vector2 velocity = (radial * centerPower
                + radial.RotatedBy(-MathHelper.PiOver2 * angle) * 1.5f) * .03f;
            Vector2 origin = p.Center + (radial - aim * 45f) * weaponScale;
            Dust dust = Dust.NewDustPerfect(origin, ModContent.DustType<T_Squash_Particle>(), velocity);
            dust.scale = particleScale * weaponScale;
            dust.noGravity = true;
            dust.color = Color.Lerp(tint, Color.White, MathF.Max(0f, swingWeight - .35f));
            dust.fadeIn = .1f + swingWeight * weaponScale;
        }
    }

    internal static void EmitHit(Vector2 center, Vector2 aim, Color tint, int hitCount, bool stars)
    {
        if (Main.dedServ || Vector2.DistanceSquared(center, Main.LocalPlayer.Center) > 2400f * 2400f) return;
        for (int i = 0; i < 14 - hitCount * 2; i++)
        {
            float angle = Main.rand.NextFloat(-.3f, .3f);
            Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<T_Squash_Particle>(),
                aim.RotatedBy(angle) * Main.rand.NextFloat(8f, 12f) * (1f - MathF.Abs(angle)));
            dust.scale = Main.rand.NextFloat(1.6f, 1.9f) * (1f - MathF.Abs(angle));
            dust.noGravity = true;
            dust.color = Color.Lerp(tint, Color.White, Main.rand.NextFloat(0f, .4f));
            dust.fadeIn = -.5f;
            if (i % 3 != 0)
            {
                Dust small = Dust.NewDustPerfect(center, DustID.FireworksRGB,
                    aim.RotatedByRandom(.6f) * Main.rand.NextFloat(5f, 10f), 0, tint, Main.rand.NextFloat(.9f, 1.4f));
                small.noGravity = true;
            }
        }
        if (stars)
            for (int i = -1; i <= 1; i++)
                CasterSparkles.Spawn(center + aim.RotatedBy(i * .6f) * 10f,
                    aim.RotatedBy(i * .6f) * 1.5f, tint, .75f, 12, aim.ToRotation() + i * .6f);
    }
}
