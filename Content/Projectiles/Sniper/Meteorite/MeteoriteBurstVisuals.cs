using System;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

internal static class MeteoriteBurstVisuals
{
    private static readonly Color Ochre = new(185, 135, 58);
    private static readonly Color Sand = new(230, 194, 119);
    internal static void DustBurst(Vector2 center, float radius)
    {
        if (Main.dedServ) return;
        int count = ArtilleryImpactVisuals.BoostCount(62);
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = (i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.12f, .12f)).ToRotationVector2();
            Vector2 velocity = direction * Main.rand.NextFloat(3f, 11f) * ArtilleryImpactVisuals.ParticleIntensity;
            Dust dust = Dust.NewDustPerfect(center + direction * Main.rand.NextFloat(5f, radius * .42f),
                i % 4 == 0 ? DustID.Dirt : DustID.Sand, velocity, 65, i % 3 == 0 ? Sand : Ochre,
                Main.rand.NextFloat(1.1f, 1.9f));
            dust.noGravity = i % 3 != 0;
            if (i % 5 == 0)
            {
                Dust smoke = Dust.NewDustPerfect(center + direction * radius * .15f, DustID.Smoke,
                    velocity * .55f - Vector2.UnitY, 140, new Color(125, 101, 65), Main.rand.NextFloat(1.4f, 2.1f));
                smoke.noGravity = true;
            }
        }
    }
    internal static void Draw(Vector2 center, float diameter, float progress)
    {
        Texture2D bloom = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Sniper/Fiammetta/Assets/FiammettaExplosionBloom").Value;
        Texture2D ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/circle_03").Value;
        float expansion = 1f - MathF.Pow(1f - progress, 3f);
        float fade = MathF.Pow(1f - progress, 1.2f);
        Vector2 position = center - Main.screenPosition;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * MathHelper.TwoPi / 6f + .2f;
            Vector2 offset = angle.ToRotationVector2() * diameter * expansion * .18f;
            Main.spriteBatch.Draw(bloom, position + offset, null, new Color(104, 78, 43) * (fade * .6f),
                angle, bloom.Size() * .5f, diameter * (.24f + expansion * .28f) / bloom.Width, SpriteEffects.None, 0f);
        }
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(bloom, position, null, Ochre * (fade * .85f), progress * .5f,
            bloom.Size() * .5f, diameter * (.3f + expansion * .6f) / bloom.Width, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(bloom, position, null, Sand * (fade * .85f), -progress,
            bloom.Size() * .5f, diameter * (.16f + expansion * .33f) / bloom.Width, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(ring, position, null, Sand * (fade * .48f), 0f, ring.Size() * .5f,
            new Vector2(diameter * (.2f + expansion * .94f), diameter * (.12f + expansion * .7f)) / ring.Size(), SpriteEffects.None, 0f);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
}
