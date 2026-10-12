using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

/// <summary>灰度火焰纹理、手动染色和模组粒子共同构成陨星炮弹的飞行层次。</summary>
internal static class MeteoriteShellVisuals
{
    private const string FlamePath = "ArknightsMod/Content/Textures/muzzle_02";
    private const string BloomPath =
        "ArknightsMod/Content/Projectiles/Sniper/Fiammetta/Assets/FiammettaExplosionBloom";
    private static readonly Color Outer = new(244, 70, 28);
    private static readonly Color Inner = new(255, 185, 86);

    internal static void SpawnFlight(Projectile projectile, int age)
    {
        if (Main.dedServ || age % 3 != 0 || ParticleManager.activeParticles.Count >= 1350) return;
        Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        Vector2 basePosition = projectile.Center - direction * 13f;
        for (int i = 0; i < 2; i++)
        {
            Vector2 position = basePosition + normal * Main.rand.NextFloat(-5f, 5f);
            Vector2 velocity = -direction * Main.rand.NextFloat(.7f, 2.2f)
                + normal * Main.rand.NextFloat(-.8f, .8f);
            new MeteoriteEmber(position, velocity, Main.rand.Next(11, 19),
                i == 0 ? Inner : Outer, Main.rand.NextFloat(13f, 24f)).Spawn();
        }
    }

    internal static void DrawTrail(Projectile projectile)
    {
        Texture2D flame = ModContent.Request<Texture2D>(FlamePath).Value;
        Texture2D bloom = ModContent.Request<Texture2D>(BloomPath).Value;
        Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
        float rotation = direction.ToRotation() - MathHelper.PiOver2;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null,
            Main.GameViewMatrix.TransformationMatrix);

        // 历史位置只保留八个点，层层衰减；火焰长度不会随速度或屏幕尺寸无限延伸。
        for (int i = projectile.oldPos.Length - 1; i >= 0; i -= 2)
        {
            if (projectile.oldPos[i] == Vector2.Zero) continue;
            float fade = 1f - i / (float)projectile.oldPos.Length;
            Vector2 center = projectile.oldPos[i] + projectile.Size * .5f - direction * 10f;
            DrawSized(flame, center, new Vector2(32f * fade, 66f * fade),
                Outer * (fade * .30f), rotation);
            DrawSized(flame, center + direction * 7f, new Vector2(13f * fade, 34f * fade),
                Inner * (fade * .48f), rotation);
        }
        DrawSized(bloom, projectile.Center - direction * 6f, new Vector2(34f),
            Inner * .42f, Main.GlobalTimeWrappedHourly * .7f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null,
            Main.GameViewMatrix.TransformationMatrix);
    }

    private static void DrawSized(Texture2D texture, Vector2 center, Vector2 size,
        Color tint, float rotation)
        => Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
}

public sealed class MeteoriteEmber : Particle
{
    private readonly Color initialColor;
    private readonly float initialSize;
    public override string TexturePath => "ArknightsMod/Content/Textures/muzzle_02";
    public override BlendState DrawBlendState => BlendState.Additive;

    public MeteoriteEmber(Vector2 position, Vector2 velocity, int lifetime, Color color, float size)
    {
        Position = position;
        Velocity = velocity;
        Lifetime = lifetime;
        initialColor = Color = color;
        initialSize = size;
        Rotation = velocity.ToRotation() - MathHelper.PiOver2;
    }

    public override void Update()
    {
        Velocity *= .90f;
        Rotation += .035f;
        Color = initialColor * MathF.Pow(1f - LifetimeRatio, 1.6f);
    }

    public override void Draw()
    {
        float size = initialSize * (1f - LifetimeRatio * .65f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, new Vector2(size / Texture.Width, size * 1.4f / Texture.Height),
            SpriteEffects.None, 0f);
    }
}
