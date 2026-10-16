using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Passenger;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Lightning_Impact_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private const int ImpactTime = 2;
    private readonly float intensity;
    private readonly int seed;
    private readonly LightningPalette colors;
    private readonly Texture2D flash;
    private readonly Texture2D ring;
    private readonly Vector2[] arcPositions = new Vector2[8];
    private readonly Vector2[] arcVelocities = new Vector2[8];
    public override string TexturePath => "ArknightsMod/Content/Textures/PassengerImpactBurst";
    public override BlendState DrawBlendState => BlendState.Additive;

    internal T_Lightning_Impact_Particle(Vector2 center, float intensity, int seed, LightningPalette? palette = null)
    {
        Position = center;
        this.intensity = intensity;
        this.seed = seed;
        colors = palette ?? LightningPalette.Passenger;
        Lifetime = 24;
        flash = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/PassengerImpactFlash").Value;
        ring = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/PassengerImpactRing").Value;
        for (int i = 0; i < arcPositions.Length; i++)
        {
            arcPositions[i] = center;
            arcVelocities[i] = (seed * .017f + MathHelper.TwoPi * i / arcPositions.Length)
                .ToRotationVector2() * (5f + i % 3) * intensity;
        }
    }

    public override void Update()
    {
        int age = Time - ImpactTime;
        if (age < 0)
            return;
        for (int i = 0; i < arcPositions.Length; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            arcVelocities[i] = arcVelocities[i].RotatedBy(-side * 0.008f
                + MathF.Sin(seed + age * 1.73f + i) * 0.012f) * 0.966f;
            arcPositions[i] += arcVelocities[i];
            if (age % 4 == 0 && age < 10 && ParticleManager.activeParticles.Count < 1800)
                new T_Chain_Bolt_Particle(arcPositions[i], arcVelocities[i] * .2f,
                    colors.Edge, (20f - age * .6f) * intensity, 8).Spawn();
        }
        float light = MathF.Pow(MathHelper.Clamp(1f - age / 12f, 0f, 1f), 2f) * intensity;
        Lighting.AddLight(Position, colors.Light * light);
    }

    public override void Draw()
    {
        if (Time < ImpactTime)
        {
            float warning = MathHelper.Clamp((Time + 0.5f) / ImpactTime, 0f, 1f);
            float warningDiameter = MathHelper.Lerp(160f, 40f, warning) * intensity;
            DrawPixels(ring, new Vector2(warningDiameter, warningDiameter * 0.42f),
                colors.Body * (MathF.Sin(warning * MathHelper.Pi) * 0.65f));
            return;
        }

        float age = Time - ImpactTime;
        float progress = MathHelper.Clamp(age / 16f, 0f, 1f);
        float expansion = 1f - MathF.Pow(1f - progress, 3f);
        float fade = 1f - MathHelper.SmoothStep(0f, 1f, progress);
        float flashFade = MathF.Pow(MathHelper.Clamp(1f - age / 9f, 0f, 1f), 1.5f);

        // 保留 Fran 的贴图叠层：橙色爆裂体、暖白亮芯、金黄外扩电环。
        DrawPixels(Texture, new Vector2(194f, 132f) * intensity * (1f + expansion * 0.16f),
            colors.Body * (flashFade * 0.94f), MathHelper.PiOver2);
        DrawPixels(flash, new Vector2(230f, 90f) * intensity,
            colors.Core * (flashFade * 0.92f));
        DrawPixels(flash, new Vector2(138f, 70f) * intensity,
            colors.Edge * (flashFade * 0.65f), MathHelper.PiOver2);

        float diameter = MathHelper.Lerp(68f, 284f, expansion) * intensity;
        Color tint = Color.Lerp(colors.Body, colors.Edge, 0.34f);
        DrawPixels(ring, new Vector2(diameter), tint * (fade * 0.76f), seed * 0.1f + expansion);
        DrawPixels(ring, new Vector2(diameter * 1.25f, diameter * 0.34f),
            colors.Edge * (fade * 0.8f));
        DrawPixels(ring, new Vector2(diameter * 0.66f),
            colors.Core * (fade * 0.40f), -seed * 0.07f - expansion);
    }

    private void DrawPixels(Texture2D texture, Vector2 size, Color tint, float rotation = 0f)
    {
        Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, tint, rotation,
            texture.Size() * 0.5f, size / texture.Size(), SpriteEffects.None, 0f);
    }
}
