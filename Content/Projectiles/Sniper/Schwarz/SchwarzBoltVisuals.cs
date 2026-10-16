using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Schwarz;

/// <summary>黑的弩的箭体由专用贴图、短距离染色拖尾和独立粒子构成。</summary>
internal static class SchwarzBoltVisuals
{
    private const string Assets = "ArknightsMod/Content/Projectiles/Sniper/Schwarz/Assets/";
    private const string Arrow = "ArknightsMod/Content/Projectiles/Sniper/Schwarz/SchwarzArrow";
    private static readonly Color Violet = new(155, 111, 210);
    private static readonly Color Pale = new(230, 218, 255);

    private static void DrawSized(Texture2D texture, Vector2 position, Vector2 size,
        Color tint, float rotation = 0f)
        => Main.spriteBatch.Draw(texture, position - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);

    private static void BeginAdditive()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    private static void EndAdditive()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullNone, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    internal static void Draw(Projectile projectile, int age)
    {
        CasterShaderTrail.Draw(projectile, new Color(33, 18, 49), Violet, 10f);
        Texture2D arrow = ModContent.Request<Texture2D>(Arrow).Value;
        Texture2D smear = ModContent.Request<Texture2D>(Assets + "SchwarzSmear").Value;
        Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        float heading = direction.ToRotation();

        // 暗色箭体保留轮廓；黑色主体由专属箭贴图而非 MagicPixel 线段承担。
        DrawSized(arrow, projectile.Center, new Vector2(13f, 43f),
            new Color(12, 8, 20), projectile.rotation);

        BeginAdditive();
        for (int i = 0; i < projectile.oldPos.Length; i += 4)
        {
            if (projectile.oldPos[i] == Vector2.Zero) continue;
            float fade = 1f - i / (float)projectile.oldPos.Length;
            Vector2 point = projectile.oldPos[i] + projectile.Size * .5f;
            DrawSized(smear, point - direction * 12f, new Vector2(38f * fade, 10f * fade),
                Violet * (.25f * fade), heading);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float wave = MathF.Sin(age * .25f + side * MathHelper.PiOver2 + projectile.identity);
            Vector2 point = projectile.Center - direction * 10f + normal * side * (7f + wave * 4f);
            DrawSized(smear, point, new Vector2(19f, 5f),
                (side < 0 ? Pale : Violet) * .62f, heading + side * .16f);
        }
        DrawSized(arrow, projectile.Center, new Vector2(10f, 37f), Violet * .54f,
            projectile.rotation);
        EndAdditive();
    }

    internal static void DrawMuzzle(Projectile projectile)
    {
        float opacity = MathF.Pow(projectile.timeLeft / 20f, 1.5f);
        Texture2D burst = ModContent.Request<Texture2D>(Assets + "SchwarzRiftImpact").Value;
        Texture2D core = ModContent.Request<Texture2D>(Assets + "SchwarzBlackCore").Value;
        DrawSized(core, projectile.Center, new Vector2(38f), Color.Black * (opacity * .55f));
        BeginAdditive();
        DrawSized(burst, projectile.Center, new Vector2(56f, 29f),
            Violet * (opacity * .56f), projectile.velocity.ToRotation());
        EndAdditive();
    }

    internal static void SpawnFlightMotes(Projectile projectile, int age)
    {
        if (ParticleManager.activeParticles.Count >= 1400) return;
        Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        for (int i = -1; i <= 1; i += 2)
        {
            float wave = MathF.Sin(age * .25f + i * MathHelper.PiOver2);
            Vector2 position = projectile.Center - direction * 9f + normal * i * (8f + wave * 3f);
            new T_Smear_Particle(position, -direction * Main.rand.NextFloat(.7f, 1.8f)
                + normal * i * .35f, Main.rand.Next(9, 15),
                i < 0 ? Pale : Violet, Main.rand.NextFloat(11f, 18f),
                height: 5f, damping: .88f, fadeExponent: 1.4f, rotation: direction.ToRotation()).Spawn();
        }
    }
}
