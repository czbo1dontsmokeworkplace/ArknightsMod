using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Pudding;

/// <summary>布丁的青色电荷、分段磁弧与逐跳节点反馈，叠加在链术士共用的完整雷弧结构上。</summary>
internal static class PuddingVisuals
{
    private const string Textures = "ArknightsMod/Content/Textures/";
    internal static readonly Color DeepCyan = new(22, 94, 151);
    internal static readonly Color Cyan = new(75, 200, 239);
    internal static readonly Color White = new(224, 251, 255);

    private static void DrawSized(Texture2D texture, Vector2 position, Vector2 size, Color color,
        float rotation = 0f)
        => Main.spriteBatch.Draw(texture, position - Main.screenPosition, null, color, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);

    internal static void DrawFlyingCore(Projectile projectile)
    {
        CasterShaderTrail.Draw(projectile, DeepCyan, Cyan, 8f);
        Texture2D flash = ModContent.Request<Texture2D>(Textures + "flare_01").Value;
        Texture2D arc = ModContent.Request<Texture2D>(Textures + "spark_07").Value;
        float rotation = projectile.velocity.ToRotation();
        DrawSized(flash, projectile.Center, new Vector2(24f, 16f), Cyan * .7f, rotation);
        DrawSized(arc, projectile.Center, new Vector2(21f, 9f), White * .88f, rotation);
    }

    internal static void Flight(Vector2 start, Vector2 end, float intensity, int phase)
    {
        if (Main.dedServ) return;
        Vector2 center = Vector2.Lerp(start, end, .65f);
        if (phase % 3 == 0)
            CasterSparkles.Spawn(center + Main.rand.NextVector2Circular(5f, 5f),
                -Vector2.UnitY * Main.rand.NextFloat(.1f, .7f), White, .48f, 10);
        if (phase % 6 == 0)
        {
            Passenger.PassengerLightningVisuals.ColoredDust(center,
                Main.rand.NextVector2Circular(.7f, .7f), Cyan, .8f);
        }
        Lighting.AddLight(center, new Vector3(.04f, .30f, .46f) * intensity);
    }

    internal static void BeginHop(Vector2 start, Vector2 end, float intensity, int seed, bool targetNode)
    {
        if (Main.dedServ || !float.IsFinite(start.X) || !float.IsFinite(end.X)
            || !float.IsFinite(start.Y) || !float.IsFinite(end.Y)) return;
        float length = Vector2.Distance(start, end);
        if (length < 3f || length > 1500f || ParticleManager.activeParticles.Count >= 1650)
            return;
        new T_Electric_Link_Particle(start, end, intensity, seed).Spawn();
        if (targetNode)
            new T_Electric_Pulse_Particle(end, .42f * intensity, false).Spawn();
    }

    internal static void AdvanceHop(Vector2 start, Vector2 end, float intensity, int phase)
    {
        if (Main.dedServ) return;
        Vector2 delta = end - start;
        int samples = Math.Clamp((int)MathF.Ceiling(delta.Length() / 32f), 1, 6);
        for (int i = 0; i < samples; i++)
        {
            Vector2 point = Vector2.Lerp(start, end, (i + .5f) / samples);
            Vector2 jitter = Main.rand.NextVector2Circular(3f, 3f);
            CasterSparkles.Spawn(point + jitter, jitter * .18f, i % 3 == 0 ? White : Cyan,
                .42f + intensity * .2f, 9, phase * .2f);
            if (i % 2 == 0 && ParticleManager.activeParticles.Count < 1550)
                new T_Streak_Particle(point, Main.rand.NextVector2Circular(1.4f, 1.4f),
                    11, .23f * intensity, Cyan, true)
                { Deformation = new Vector2(.7f, 1.05f) }.Spawn();
        }
    }

    internal static void Impact(Vector2 center, float intensity, int seed, bool firstHop)
    {
        if (Main.dedServ) return;
        if (ParticleManager.activeParticles.Count < 1550)
            new T_Electric_Pulse_Particle(center, intensity * (firstHop ? 1.15f : .85f), true).Spawn();
        int sparks = firstHop ? 10 : 6;
        for (int i = 0; i < sparks; i++)
        {
            float angle = MathHelper.TwoPi * i / sparks + seed * .002f;
            Vector2 direction = angle.ToRotationVector2();
            CasterSparkles.Spawn(center + direction * 6f, direction * Main.rand.NextFloat(1f, 3f),
                i % 3 == 0 ? White : Cyan, Main.rand.NextFloat(.5f, .9f),
                Main.rand.Next(10, 17), angle);
            if (i % 2 == 0 && ParticleManager.activeParticles.Count < 1600)
                new T_Streak_Particle(center + direction * 5f, direction * Main.rand.NextFloat(2f, 5f),
                    Main.rand.Next(10, 17), .34f * intensity, i % 3 == 0 ? White : Cyan, true)
                { Deformation = new Vector2(.5f, 1.45f) }.Spawn();
        }
        for (int i = 0; i < (firstHop ? 3 : 2); i++)
        {
            Passenger.PassengerLightningVisuals.ColoredDust(center,
                Main.rand.NextVector2Circular(3f, 3f), Cyan, .9f * intensity);
        }
        SoundEngine.PlaySound(SoundID.Item93 with
            { Volume = firstHop ? .28f : .17f, Pitch = firstHop ? .35f : .55f, MaxInstances = 4 }, center);
    }
}

/// <summary>每条磁弧只连接既定的两个节点；每个纹理片段都按实际尺寸归一化。</summary>


/// <summary>电荷和弹跳节点的收缩/扩散脉冲，由本模组粒子管理器更新。</summary>
