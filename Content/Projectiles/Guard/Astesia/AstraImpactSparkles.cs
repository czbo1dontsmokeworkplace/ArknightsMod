using ArknightsMod.Common.Particle.T;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;

namespace ArknightsMod.Content.Projectiles.Guard.Astesia;

// Original vanilla LightingBolts pattern inspired by the local CLCBLightingBoltsSystem:
// pooled PrettySparkleParticles with actual velocity, never Calamity particles or Dust.
internal static class AstraImpactSparkles
{
    private static readonly ParticlePool<T_Lighting_Bolts_Particle> Pool = new(256, () => new T_Lighting_Bolts_Particle());
    private const float GoldenAngle = 2.39996323f;

    internal static void Burst(Vector2 point, Vector2 attackDirection, Color sky, float targetRadius)
    {
        if (Main.dedServ || Main.gameMenu || !float.IsFinite(point.X + point.Y)
            || point.X < Main.screenPosition.X - 160f || point.X > Main.screenPosition.X + Main.screenWidth + 160f
            || point.Y < Main.screenPosition.Y - 160f || point.Y > Main.screenPosition.Y + Main.screenHeight + 160f)
            return;

        float seed = attackDirection.ToRotation() + Main.rand.NextFloat(-.2f, .2f);
        // Thirty-two moving rays form the burst; the golden angle avoids rigid spokes.
        // Speeds and lifetimes keep their travel within roughly 140 world pixels.
        for (int i = 0; i < 32; i++)
        {
            float angle = seed + i * GoldenAngle + Main.rand.NextFloat(-.07f, .07f);
            Vector2 radial = angle.ToRotationVector2();
            float speed = Main.rand.NextFloat(3.2f, 6.2f);
            int life = Main.rand.Next(12, 20);
            Color tint = i % 6 == 0 ? Color.Lerp(sky, Color.White, .74f)
                : Color.Lerp(sky, new Color(190, 235, 255), Main.rand.NextFloat(.12f, .45f));
            Spawn(point + radial * Main.rand.NextFloat(2f, 4f + targetRadius * .18f),
                radial * speed, tint, Main.rand.NextFloat(1.25f, 2.05f),
                Main.rand.NextFloat(.32f, .62f), life, angle, .68f);
        }

        // A smaller, slower inner layer fills the impact without obscuring the NPC.
        for (int i = 0; i < 8; i++)
        {
            float angle = seed + MathHelper.TwoPi * (i + .5f) / 8f;
            Vector2 radial = angle.ToRotationVector2();
            Spawn(point + radial * 3f, radial * Main.rand.NextFloat(1.5f, 3f),
                Color.Lerp(sky, Color.White, .42f), .9f, .42f,
                Main.rand.Next(10, 15), angle, .55f);
        }

        float axis = attackDirection.ToRotation();
        for (int i = 0; i < 2; i++)
            Spawn(point, Vector2.Zero, Color.Lerp(sky, Color.White, .72f),
                2.15f, .34f, 9, axis + i * MathHelper.PiOver2, .78f);
    }

    private static void Spawn(Vector2 point, Vector2 velocity, Color tint, float length,
        float width, int life, float rotation, float additive)
    {
        T_Lighting_Bolts_Particle sparkle = Pool.RequestParticle();
        sparkle.LocalPosition = point;
        sparkle.Velocity = velocity;
        sparkle.ColorTint = tint;
        sparkle.Rotation = rotation;
        sparkle.Scale = new Vector2(length, width);
        sparkle.TimeToLive = life;
        sparkle.FadeInNormalizedTime = .02f;
        sparkle.FadeOutNormalizedTime = .82f;
        sparkle.FadeInEnd = 2;
        sparkle.FadeOutStart = Math.Max(3, life / 3);
        sparkle.FadeOutEnd = life;
        sparkle.AdditiveAmount = additive;
        Main.ParticleSystem_World_OverPlayers.Add(sparkle);
    }
}
