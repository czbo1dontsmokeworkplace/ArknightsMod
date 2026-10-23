using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Calamity EssenceFire的旋转双层火焰 + MeowFire的HeavySmoke动画。
// 只借用灰度材质与组成方式，颜色、生命周期和敌对判定均由本模组管理。
internal static class EvolutionArsenalVisuals
{
    private static ulong smokeTick;
    private static int smokeCount;
    internal static void Smoke(Vector2 center, Vector2 velocity, float diameter, int lifetime, bool hot = false, Color? theme = null, float opacity = 1)
    {
        if (Main.dedServ || EvolutionCinematics.Reduced && Main.rand.NextBool()) return;
        if (smokeTick != Main.GameUpdateCount) { smokeTick = Main.GameUpdateCount; smokeCount = 0; }
        if (++smokeCount > 18) return;
        new EvolutionFlameSmoke(center, velocity, diameter, lifetime, hot, theme ?? EvolutionVisuals.Blood, opacity).Spawn();
    }
    internal static void Flame(Vector2 center, Vector2 direction, float age, float opacity, float length = 90)
    {
        Texture2D fire = EvolutionVisuals.Asset("ArsenalFlame"), mist = EvolutionVisuals.Asset("ArsenalMist");
        for (int i = 5; i >= 0; i--)
        {
            float u = i / 5f;
            float spin = age * .08f * (i % 2 == 0 ? 1 : -1) + i * 1.7f;
            Vector2 position = center - direction * (u * length) - Main.screenPosition;
            float size = (22 + 38 * u) * (1 - u * .35f);
            Color tint = Color.Lerp(EvolutionVisuals.Core, EvolutionVisuals.Blood, u);
            Main.spriteBatch.Draw(fire, position, null, (tint with { A = 0 }) * ((1 - u * .75f) * opacity * .65f),
                spin, fire.Size() * .5f, new Vector2(size * 1.1f, size) / fire.Size(), SpriteEffects.None, 0);
            Rectangle frame = mist.Frame(1, 3, 0, i % 3);
            Main.spriteBatch.Draw(mist, position, frame, (EvolutionVisuals.Blood with { A = 0 }) * ((1 - u) * opacity * .25f),
                -spin, frame.Size() * .5f, new Vector2(size * 1.6f) / frame.Size(), SpriteEffects.None, 0);
        }
    }
    internal static void Spark(Vector2 start, Vector2 end, float width, Color color)
    {
        Texture2D spark = EvolutionVisuals.Asset("ArsenalSpark");
        Vector2 delta = end - start;
        if (delta.LengthSquared() < .1f) return;
        Main.spriteBatch.Draw(spark, (start + end) * .5f - Main.screenPosition, null, color with { A = 0 },
            delta.ToRotation() + MathHelper.PiOver2, spark.Size() * .5f,
            new Vector2(width * 4, delta.Length() + width) / spark.Size(), SpriteEffects.None, 0);
    }
    internal static void Burst(Vector2 center, float power, float spatialScale = 1, float density = 1, float opacity = 1, float intensity = 1)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < (int)MathF.Round((EvolutionCinematics.Reduced ? 4 : 10) * density); i++)
            Smoke(center, (Main.rand.NextVector2Circular(5, 4) * power - Vector2.UnitY) * spatialScale * intensity,
                (55 + Main.rand.NextFloat(30)) * power * spatialScale * intensity, 34, i % 3 == 0, opacity: opacity);
        EvolutionVisuals.Burst(center, power, false, spatialScale: spatialScale, density: density, opacity: opacity, intensity: intensity);
    }
    internal static void Blast(Vector2 center, int age, float radius, Color? theme = null, float opacity = 1, float intensity = 1)
    {
        Texture2D burst = EvolutionVisuals.Asset("ArsenalBurst");
        float t = Math.Clamp(age / 24f, 0, 1);
        Main.spriteBatch.Draw(burst, center - Main.screenPosition, null,
            ((theme ?? EvolutionVisuals.Blood) with { A = 0 }) * (1 - t) * .8f * opacity, t * .2f,
            burst.Size() * .5f, new Vector2(radius * 2 * (.4f + .6f * t) * intensity) / burst.Size(), SpriteEffects.None, 0);
        EvolutionVisuals.Glow(center, EvolutionVisuals.Core * ((1 - t) * .7f * opacity), new Vector2(radius * (1 - t) * intensity));
    }
    internal static void GravityFlare(Vector2 center, float rotation, float opacity)
    {
        Texture2D texture = EvolutionVisuals.Asset("EvolutionGravityFlare");
        // 原创图的亮核在右侧；用亮核而非整张画布的中心对齐实际碰撞体。
        Vector2 origin = new(texture.Width * .74f, texture.Height * .54f);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, new Color(248, 75, 86) * opacity,
            rotation, origin, new Vector2(64, 45) / texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, (EvolutionVisuals.Core with { A = 0 }) * (opacity * .45f),
            rotation, origin, new Vector2(64, 45) / texture.Size(), SpriteEffects.None, 0);
    }
}

// 自己的粒子生命周期与批次：不依赖灾厄运行时或其ParticleHandler。
public sealed class EvolutionFlameSmoke : Particle
{
    public override string TexturePath => EvolutionVisuals.Root + "ArsenalSmoke";
    private readonly int variant;
    private readonly float diameter, spin, opacity;
    private readonly bool hot;
    private readonly Color theme;
    public EvolutionFlameSmoke(Vector2 center, Vector2 velocity, float diameter, int lifetime, bool hot, Color theme, float opacity = 1)
    {
        Position = center; Velocity = velocity; this.diameter = diameter; Lifetime = lifetime; this.hot = hot; this.theme = theme; this.opacity = opacity;
        variant = Main.rand.Next(7); spin = Main.rand.NextFloat(-.04f, .04f); Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update() { Velocity *= .89f; Velocity.Y -= .04f; Rotation += spin; }
    public override void Draw()
    {
        float t = Math.Clamp(LifetimeRatio, 0, 1);
        Rectangle frame = new(variant * 80, Math.Min(5, (int)(t * 6)) * 80, Math.Min(80, Texture.Width - variant * 80), 80);
        float fade = Math.Min(1, t * 6) * MathF.Pow(1 - t, 1.3f);
        float size = diameter * (.8f + t * .65f);
        Color tint = Color.Lerp(hot ? Color.Lerp(theme, EvolutionVisuals.Core, .75f) : theme, theme * .35f, t);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame,
            (tint with { A = 0 }) * (fade * (hot ? .5f : .26f) * opacity), Rotation, frame.Size() * .5f,
            new Vector2(size) / frame.Size(), SpriteEffects.None, 0);
    }
}
