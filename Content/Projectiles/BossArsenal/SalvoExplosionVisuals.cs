using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class SalvoExplosionVisuals
{
    private static Texture2D volume;
    internal static void Unload() { volume?.Dispose(); volume = null; }
    // A smooth density field gives the blast solid fire/smoke volume, without
    // reusing the magic-circle details in the generic StarCore mask.
    private static void Blob(Vector2 at, Vector2 size, Color color, float angle = 0)
    {
        if (volume == null) {
            volume = new Texture2D(Main.graphics.GraphicsDevice, 64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) {
                float r = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).LengthSquared();
                pixels[y * 64 + x] = new Color(255, 255, 255, (int)(255 * MathF.Pow(Math.Max(0, 1 - r), 2)));
            }
            volume.SetData(pixels);
        }
        Main.EntitySpriteDraw(volume, at - Main.screenPosition, null, color, angle, new Vector2(32), size / 64, SpriteEffects.None);
    }
    internal static void Burst(Vector2 at)
    {
        for (int i = 0; i < 58 && ParticleManager.activeParticles.Count < 1200; i++) {
            Vector2 ray = (i * 2.399963f + Main.rand.NextFloat(-.1f, .1f)).ToRotationVector2();
            new T_SalvoDebris(at + ray * Main.rand.NextFloat(0, 25), ray * Main.rand.NextFloat(5, 19),
                Main.rand.NextFloat(2, 6)).Spawn();
        }
        for (int i = 0; i < 72; i++) {
            Vector2 ray = (i * 2.399963f).ToRotationVector2();
            bool smoke = i % 3 == 0;
            Dust d = Dust.NewDustPerfect(at + ray * Main.rand.NextFloat(0, 55), smoke ? DustID.Smoke : DustID.Torch,
                ray * Main.rand.NextFloat(3, 14), smoke ? 100 : 0, default, smoke ? 2.3f : 1.7f);
            d.noGravity = !smoke;
        }
    }
    internal static void Draw(Vector2 at, float t, float radius, int seed)
    {
        t = Math.Clamp(t, 0, 1);
        float bloom = MathF.Sin(MathHelper.Pi * MathF.Sqrt(t));
        float fade = (1 - t) * (1 - t);
        float angle = seed * .731f;
        // Dark lobes define the volume behind the incandescent core.
        using (var pass = new ChargedOperatorVfx.Pass(false)) {
            for (int i = 0; i < 9; i++) {
                Vector2 ray = (angle + i * 2.399963f).ToRotationVector2();
                float spread = radius * (.12f + .5f * MathF.Sqrt(t));
                Vector2 center = at + ray * spread - Vector2.UnitY * (t * 26);
                Blob(center, new Vector2(110 + 140 * t), new Color(12, 8, 14) * (fade * .9f), angle + i);
            }
        }
        using (var pass = new ChargedOperatorVfx.Pass(true)) {
            // Two expanding fronts, followed by a hot, irregular fireball.
            float front = 1 - MathF.Pow(1 - t, 3);
            ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(radius * 2 * front),
                new Color(255, 120, 45) * fade, angle);
            float second = Math.Clamp((t - .09f) / .7f, 0, 1);
            ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(radius * 1.65f * MathF.Sqrt(second)),
                new Color(255, 215, 140) * (fade * .7f), -angle);
            for (int i = 0; i < 11; i++) {
                Vector2 ray = (angle + i * 2.399963f).ToRotationVector2();
                Vector2 center = at + ray * (radius * .38f * front);
                float size = (72 + i % 3 * 22) * bloom;
                ChargedOperatorVfx.Sprite("ForwardFlame", center, new Vector2(size * 1.7f, size),
                    new Color(255, 80 + i % 3 * 30, 18) * (fade * .75f), ray.ToRotation());
                Blob(center, new Vector2(size * 1.4f), new Color(255, 90, 20) * (fade * .85f));
                Blob(center, new Vector2(size * .8f), new Color(255, 215, 120) * (fade * .7f));
            }
            for (int i = 0; i < 18; i++) {
                Vector2 ray = (angle + i * MathHelper.TwoPi / 18).ToRotationVector2();
                Vector2 center = at + ray * (radius * front * (.7f + i % 3 * .1f));
                ChargedOperatorVfx.Sprite("LinearSpark", center, new Vector2(32 * fade + 3, 4),
                    new Color(255, 160, 60) * fade, ray.ToRotation());
            }
            float flash = Math.Max(0, 1 - t / .18f);
            Blob(at, new Vector2(240, 210) * bloom, new Color(255, 125, 35) * fade);
            Blob(at, new Vector2(150 + front * 90), Color.White * (flash * .9f));
            ChargedOperatorVfx.Sprite("LinearSpark", at, new Vector2(420, 28) * flash,
                new Color(255, 230, 190) * flash, -.12f);
        }
        Lighting.AddLight(at, new Vector3(1.7f, .48f, .12f) * fade);
    }
}

public sealed class SalvoExplosionResources : ModSystem
{
    public override void Unload() => Main.QueueMainThreadAction(SalvoExplosionVisuals.Unload);
}
