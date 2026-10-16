using ArknightsMod.Common.Particle.T;
using System;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

// References: SupernovaStealthBoom's implosion / directional sparks / smoke, and
// DestructionStar.Explode's layered bloom, expanding ring and lingering dark volume.
// Azafure, LLC: https://github.com/CalamityTeam/CalamityModPublic
// Reimplemented with local resources and Arknights Particle; no runtime dependency.
internal static class MostimaClockExplosion
{
    internal static void Draw(Vector2 center, float age, float fade = 1f, float scale = 1f, float gold = 0f)
    {
        if (MostimaClockwork.BeatAge(age) < 0f || !SpreadVfxBudget.Visible(center, 680f * scale)) return;
        float beat = MostimaClockwork.BeatAge(age);
        float t = beat / MostimaClockwork.BeatTicks;
        float grow = 1f - MathF.Pow(1f - t, 4f);
        float diameter = MostimaClockwork.BlastRadius * 2f * scale;
        float burst = MathF.Pow(1f - t, 1.7f) * fade;
        using (MostimaVisuals.Begin(BlendState.NonPremultiplied))
        {
            MostimaVisuals.Sprite("TemporalSmear", center, new Vector2(diameter * (.62f + .32f * grow)),
                Color.Lerp(new Color(22, 13, 55), new Color(86, 40, 9), gold) * (.55f * (1f - t) * fade), age * .008f);
        }
        using (SpreadArtsMaterial.Begin())
        {
            // Textured explosion faces, a broad shock front and a very short light core.
            SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(diameter * (.26f + .7f * grow)),
                OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaVisuals.Blue, gold), burst * .75f), age * .023f);
            SpreadArtsMaterial.Sprite("ImpactPetals", center, new Vector2(diameter * (.2f + .55f * grow)),
                OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaVisuals.Violet, gold), burst * .6f), -age * .017f);
            SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(diameter * (.5f - t * .3f)),
                OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaVisuals.Blue, gold), burst * .8f));
            SpreadArtsMaterial.Sprite("ChargeBloom", center, new Vector2(diameter * .26f),
                OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaClockwork.Silver, gold), MathF.Max(0f, 1f - t * 4f) * .8f * fade));
        }
        // Show the full damaging perimeter immediately, even before the textured wave arrives.
        for (int i = 0; i < MostimaGrandClockVfx.DecorationCount; i++)
            for (int j = 0; j < 8; j++)
            {
                float arcStep = .0275f * 12f / MostimaGrandClockVfx.DecorationCount;
                float a = i * MathHelper.TwoPi / MostimaGrandClockVfx.DecorationCount + j * arcStep;
                MostimaClockwork.Line(center + a.ToRotationVector2() * MostimaClockwork.BlastRadius * scale,
                    center + (a + arcStep).ToRotationVector2() * MostimaClockwork.BlastRadius * scale,
                    OperatorFlightVfx.Add(MostimaClockwork.TimeColor(MostimaVisuals.Blue, gold), burst * .6f), 2f);
            }
    }
    internal static void Emit(Vector2 center, int beat, float scale = 1f, float gold = 0f, bool intensified = false)
    {
        for (int i = 0; i < (intensified ? 36 : 24); i++)
        {
            if (!SpreadVfxBudget.Take(center, 440f)) break;
            bool smoke = i % 4 == 0;
            float angle = i * MathHelper.TwoPi / (intensified ? 36f : 24f) + beat * .17f;
            Vector2 ray = angle.ToRotationVector2();
            new T_Clock_BlastFragment_Particle(center + ray * 24f * scale, ray * (smoke ? 5f : 12f + i % 3 * 5f) * scale,
                MostimaClockwork.TimeColor(i % 3 == 0 ? MostimaVisuals.Violet : MostimaVisuals.Blue, gold), smoke).Spawn();
        }
    }
}
