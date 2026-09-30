using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Medic.Warfarin;

// Purely visual Unstable Plasma material. Every element is a bounded sprite, never a world-length beam.
internal static class WarfarinPlasmaVisuals
{
    private static readonly Color Red = new(218, 9, 54, 0);
    private static readonly Color Dark = new(76, 2, 20, 0);
    private static readonly Color White = new(255, 199, 215, 0);

    internal static void ActivationBurst(Vector2 center)
    {
        if (Main.dedServ)
            return;
        for (int i = 0; i < 72; i++)
        {
            float angle = MathHelper.TwoPi * i / 72f + Main.rand.NextFloat(-.06f, .06f);
            int type = i % 9 == 0 ? DustID.GemRuby : i % 4 == 0 ? DustID.RedTorch : DustID.Blood;
            Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), type,
                angle.ToRotationVector2() * Main.rand.NextFloat(2.5f, 8.4f), 25,
                i % 7 == 0 ? new Color(255, 170, 196) : default, Main.rand.NextFloat(.85f, 1.55f));
            dust.noGravity = true;
            if (i % 6 == 0) dust.fadeIn = 1.25f;
        }
        for (int i = 0; i < 18; i++)
        {
            Dust smoke = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(12f, 9f), DustID.Smoke,
                Main.rand.NextVector2Circular(3.2f, 2.1f) + new Vector2(0f, -1.2f), 110,
                new Color(116, 4, 31), Main.rand.NextFloat(.75f, 1.25f));
            smoke.noGravity = true;
        }
    }

    internal static void EmitAuraParticles(Vector2 center, float age)
    {
        if (Main.dedServ || ((int)age & 1) != 0)
            return;
        float time = age * .06f;
        for (int i = 0; i < 3; i++)
        {
            float angle = time * (i == 0 ? 1f : -.72f) + i * MathHelper.TwoPi / 3f;
            Vector2 point = center + angle.ToRotationVector2() * new Vector2(44f, 29f);
            int type = ((int)age + i) % 5 == 0 ? DustID.RedTorch : DustID.Blood;
            Dust dust = Dust.NewDustPerfect(point, type, angle.ToRotationVector2() * .4f + new Vector2(0f, -.25f),
                28, i == 2 ? new Color(255, 160, 190) : default, i == 2 ? .95f : .72f);
            dust.noGravity = true;
        }
    }

    internal static void DrawActivationPulse(Vector2 center, float age, float fade)
    {
        if (age > 38f)
            return;
        Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        Vector2 origin = glow.Size() * .5f;
        float progression = age / 38f;
        for (int layer = 0; layer < 4; layer++)
        {
            float shifted = MathHelper.Clamp(progression - layer * .13f, 0f, 1f);
            if (shifted <= 0f) continue;
            float size = MathHelper.Lerp(22f, 138f, shifted);
            Main.EntitySpriteDraw(glow, center - Main.screenPosition, null, (layer == 0 ? White : Red) * fade * (1f - shifted) * .62f,
                0f, origin, new Vector2(size, size * .64f) / glow.Size(), SpriteEffects.None);
        }
    }

    internal static void DrawAuraParticles(Vector2 center, float age, float fade, float pulse)
    {
        Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        Vector2 origin = glow.Size() * .5f;
        float time = age * .035f;
        // Counter-rotating blood cells make the reservoir feel alive during the whole buff.
        for (int stream = 0; stream < 2; stream++)
        for (int i = 0; i < 15; i++)
        {
            float phase = i / 15f * MathHelper.TwoPi + time * (stream == 0 ? 1f : -1.35f);
            float wave = .82f + .18f * MathF.Sin(phase * 3f + age * .09f);
            Vector2 position = center + phase.ToRotationVector2() * new Vector2(45f * wave, 29f * wave);
            float brightness = .2f + .8f * MathF.Pow(Math.Max(0f, MathF.Sin(phase * 2f - age * .07f)), 6f);
            Color color = i % 5 == 0 ? White : stream == 0 ? Red : Dark;
            float radius = (i % 5 == 0 ? 5f : 3.2f) * (stream == 0 ? pulse : 1f);
            Main.EntitySpriteDraw(glow, position - Main.screenPosition, null, color * fade * brightness,
                phase + MathHelper.PiOver2, origin, new Vector2(radius, radius) / glow.Size(), SpriteEffects.None);
        }
        // Compact plasma ampoules above the player, retained alongside the existing monitor lines.
        for (int i = 0; i < 3; i++)
        {
            float angle = -age * .025f + i * MathHelper.TwoPi / 3f;
            Vector2 point = center + angle.ToRotationVector2() * new Vector2(41f, 27f);
            Main.EntitySpriteDraw(glow, point - Main.screenPosition, null, White * fade * .72f,
                0f, origin, new Vector2(7f, 2f) / glow.Size(), SpriteEffects.None);
            Main.EntitySpriteDraw(glow, point - Main.screenPosition, null, White * fade * .72f,
                MathHelper.PiOver2, origin, new Vector2(7f, 2f) / glow.Size(), SpriteEffects.None);
        }
    }

    internal static void DrawDressingDetails(Projectile projectile, float age, bool returning)
    {
        Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        Vector2 origin = glow.Size() * .5f;
        float fade = MathHelper.Clamp(projectile.timeLeft / 16f, 0f, 1f);
        for (int i = 0; i < 4; i++)
        {
            float angle = age * (returning ? -.16f : .11f) + i * MathHelper.PiOver2;
            Vector2 point = projectile.Center + angle.ToRotationVector2() * new Vector2(15f, 9f);
            Color color = i == 0 || returning ? White : Red;
            Main.EntitySpriteDraw(glow, point - Main.screenPosition, null, color * fade * .65f,
                angle, origin, new Vector2(4f, 2.5f) / glow.Size(), SpriteEffects.None);
        }
        for (int i = 0; i < 4; i++)
        {
            Vector2 point = projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitX) * (7f + i * 6f);
            Main.EntitySpriteDraw(glow, point - Main.screenPosition, null, Dark * fade * (1f - i / 5f) * .52f,
                projectile.rotation, origin, new Vector2(5f - i * .6f, 3f) / glow.Size(), SpriteEffects.None);
        }
    }

    internal static void DressingBurst(Vector2 center)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 36; i++)
        {
            int type = i % 6 == 0 ? DustID.RedTorch : i % 4 == 0 ? DustID.GemRuby : DustID.Blood;
            Dust dust = Dust.NewDustPerfect(center, type, Main.rand.NextVector2Circular(4.5f, 3.8f), 20,
                i % 5 == 0 ? new Color(255, 186, 208) : default, Main.rand.NextFloat(.8f, 1.4f));
            dust.noGravity = true;
        }
    }
}
