using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Passenger;

/// <summary>
/// 取自 Fran 的 FranFlatLightningEffects / FranArcParticle / FranFissionArcParticle。
/// 保留空间采样、黄金角扰动、裂纹展开和冷却余弧，改为异客的橙黄配色。
/// </summary>
internal static class PassengerLightningVisuals
{
    internal static readonly Color Body = new(255, 145, 28);
    internal static readonly Color Edge = new(255, 218, 88);
    internal static readonly Color Core = new(255, 247, 210);
    private const float GoldenAngle = 2.39996323f;

    private static bool Visible(Vector2 start, Vector2 end) => !Main.dedServ && !Main.gameMenu
        && Math.Max(start.X, end.X) >= Main.screenPosition.X - 700f
        && Math.Min(start.X, end.X) <= Main.screenPosition.X + Main.screenWidth + 700f
        && Math.Max(start.Y, end.Y) >= Main.screenPosition.Y - 700f
        && Math.Min(start.Y, end.Y) <= Main.screenPosition.Y + Main.screenHeight + 700f;

    internal static void SpawnArc(IEntitySource source, int owner, Vector2 start, Vector2 end, float intensity)
    {
        if (owner == Main.myPlayer)
            Projectile.NewProjectile(source, start, end - start, ModContent.ProjectileType<PassengerArcVisual>(),
                0, 0f, owner, intensity);
    }

    internal static void Fracture(Vector2 start, Vector2 end, float intensity, int seed,
        float growth = 3f, int lifetime = 16, LightningPalette? palette = null)
    {
        float distance = Vector2.Distance(start, end);
        if (!Visible(start, end) || !float.IsFinite(distance) || distance < 7f || distance > 2400f
            || ParticleManager.activeParticles.Count >= 2200)
            return;
        new T_Lightning_Fracture_Particle(start, end, (palette ?? LightningPalette.Passenger).Body, MathHelper.Clamp(2.5f * intensity, 0.8f, 6.5f),
            seed, lifetime, growth).Spawn();
    }

    internal static void Burst(Vector2 center, float intensity, int seed, LightningPalette? palette = null)
    {
        if (!Visible(center, center))
            return;
        float phase = seed * GoldenAngle;
        for (int i = 0; i < 4; i++)
        {
            Vector2 reach = (phase + MathHelper.TwoPi * i / 4f).ToRotationVector2()
                * (48f + 20f * intensity);
            Fracture(center + reach.SafeNormalize(Vector2.UnitX) * 7f,
                center + reach, intensity, seed + i * 397, 2.5f, 14, palette);
            EmitPoint(center + reach * 0.55f, reach, intensity, i * 2, palette);
        }
    }

    internal static void PrepareImpact(Vector2 center, float intensity, int seed, LightningPalette? palette = null)
    {
        if (Visible(center, center) && ParticleManager.activeParticles.Count < 2200)
            new T_Lightning_Impact_Particle(center, intensity, seed, palette).Spawn();
    }

    /// <summary>只在真实二跳及后续节点加两条短裂弧，强化弹跳瞬间而不增加伤害弹幕。</summary>
    internal static void JumpAccent(Vector2 start, Vector2 end, float intensity, int seed,
        LightningPalette palette)
    {
        if (!Visible(start, end) || ParticleManager.activeParticles.Count >= 1750)
            return;
        Vector2 incoming = (end - start).SafeNormalize(Vector2.UnitX);
        Vector2 normal = incoming.RotatedBy(MathHelper.PiOver2);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 forkEnd = end + (incoming * 15f + normal * side * 24f) * intensity;
            Fracture(end, forkEnd, intensity * .62f, seed + side * 113, 2.2f, 13, palette);
        }
    }

    internal static void Impact(Vector2 center, float intensity, int seed, bool mainStrike, LightningPalette? palette = null)
    {
        LightningPalette colors = palette ?? LightningPalette.Passenger;
        if (!Visible(center, center))
            return;
        // 环向爆裂：不压缩垂直分量，电弧覆盖完整圆周。
        int fragments = mainStrike ? 14 : 8;
        for (int i = 0; i < fragments && ParticleManager.activeParticles.Count < 1800; i++)
        {
            Vector2 direction = (seed * 0.017f + i * GoldenAngle).ToRotationVector2();
            new T_Streak_Particle(center, direction * (4f + i % 5) * intensity,
                14 + i % 5, 0.60f * intensity, i % 4 == 0 ? colors.Core : colors.Edge, true)
                { Deformation = new Vector2(0.35f, 2.4f) }.Spawn();
        }
        for (int i = 0; i < 4; i++)
        {
            Vector2 reach = (seed * .017f + MathHelper.TwoPi * i / 4f).ToRotationVector2()
                * (70f + i * 12f) * intensity;
            Fracture(center, center + reach, intensity * 0.7f, seed + i * 397, 2.5f, 16, palette);
        }
        // Dust 仅作少量点缀，主体仍由爆裂贴图、扩散环和自定义电弧承担。
        for (int i = 0; i < (mainStrike ? 4 : 2); i++)
        {
            ColoredDust(center, Main.rand.NextVector2Circular(4f, 4f), colors.Edge, intensity);
        }
        if (mainStrike)
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, Pitch = -0.22f, MaxInstances = 3 }, center);
    }

    internal static void ElectricImpactDust(Vector2 center, float intensity, bool firstHit, LightningPalette? palette = null)
    {
        if (!Visible(center, center))
            return;
        // 只叠加在真实撞敌后的传导爆闪，不用于飞行或三技能，避免沿途凭空爆炸。
        for (int i = 0; i < (firstHit ? 8 : 4); i++)
        {
            Vector2 velocity = (MathHelper.TwoPi * i / (firstHit ? 8f : 4f)
                + Main.rand.NextFloat(-0.15f, 0.15f)).ToRotationVector2()
                * Main.rand.NextFloat(2.5f, 6.5f) * intensity;
            ColoredDust(center, velocity, (palette ?? LightningPalette.Passenger).Edge,
                Main.rand.NextFloat(.85f, 1.25f) * intensity);
        }
    }

    // FireworksRGB is used by tModLoader's ExampleMod for explicitly colored dust.
    // Do not reuse Electric's atlas frame with a different ModDust type.
    internal static void ColoredDust(Vector2 center, Vector2 velocity, Color color, float scale)
    {
        if (!Visible(center, center)) return;
        Dust dust = Dust.NewDustPerfect(center, DustID.FireworksRGB, velocity, 0, color, scale);
        dust.color = color;
        dust.noGravity = true;
        dust.noLight = true;
    }

    internal static void EmitSpan(Vector2 start, Vector2 end, float intensity, int phase, LightningPalette? palette = null)
    {
        if (!Visible(start, end) || ParticleManager.activeParticles.Count >= 1800)
            return;
        float length = Vector2.Distance(start, end);
        if (!float.IsFinite(length) || length < 0.5f)
            return;
        int samples = Math.Clamp((int)MathF.Ceiling(length / 24f), 1, 28);
        Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
        for (int i = 0; i < samples; i++)
            EmitPoint(Vector2.Lerp(start, end, (i + 0.5f) / samples), direction,
                intensity, phase + i * 2, palette);
    }

    private static void EmitPoint(Vector2 center, Vector2 direction, float intensity, int phase, LightningPalette? palette = null)
    {
        LightningPalette colors = palette ?? LightningPalette.Passenger;
        if (ParticleManager.activeParticles.Count >= 1800)
            return;
        direction = direction.SafeNormalize(Vector2.UnitY);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        float oscillation = MathF.Sin(phase * GoldenAngle);
        Color color = phase % 5 == 0 ? colors.Core : Color.Lerp(colors.Body, colors.Edge, 0.5f + oscillation * 0.22f);
        Vector2 offset = normal * oscillation * 7f * intensity;
        Vector2 drift = -direction * (0.28f + intensity * 0.16f) + normal * oscillation * 0.36f;
        if ((phase & 1) == 0)
            new T_Lightning_AfterArc_Particle(center + offset, drift,
                direction.RotatedBy(oscillation * 0.22f), color,
                (48f + 18f * MathF.Abs(oscillation)) * intensity,
                (30f + 8f * MathF.Abs(oscillation)) * intensity, 10 + Math.Abs(phase % 4)).Spawn();
        if (phase % 5 == 0)
            new T_Streak_Particle(center, drift * 1.2f, 6, 0.35f * intensity,
                Color.Lerp(color, colors.Core, 0.38f), true).Spawn();
    }
}
