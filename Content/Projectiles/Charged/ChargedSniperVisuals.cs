using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;

namespace ArknightsMod.Content.Projectiles.Charged;

internal static class ChargedSniperVisuals
{
    private const string Textures = "ArknightsMod/Content/Textures/ChargedSniper/";
    private static readonly Dictionary<string, Texture2D> textures = new();
    private static Effect spreadShader, lineShader;
    private static Texture2D Texture(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D texture))
            textures[name] = texture = ModContent.Request<Texture2D>(Textures + name, AssetRequestMode.ImmediateLoad).Value;
        return texture;
    }
    internal static void Unload()
    {
        // These assets belong to tML; only discard our references.
        textures.Clear(); spreadShader = lineShader = null;
    }

    internal static float ScanLength(Vector2 start, Vector2 aim, ChargedOperatorKind kind, int skill)
    {
        float maximum = ChargedOperatorRules.ShotRange(kind, skill);
        if (kind == ChargedOperatorKind.Ambriel && skill == 2) return maximum;
        if (Collision.SolidCollision(start - Vector2.One, 2, 2)) return 0;
        float[] samples = new float[3];
        Collision.LaserScan(start, aim, 1, maximum, samples);
        return Math.Min(samples[0], Math.Min(samples[1], samples[2]));
    }

    // Port of AMRAimScope's SpreadTelegraph / PixelatedSightLine passes, using local assets.
    internal static void DrawSight(Vector2 muzzle, Vector2 aim, ChargedOperatorKind kind, int skill, float charge)
    {
        Color color = ChargedOperatorRules.ColorFor(kind);
        float radius = MathHelper.Lerp(400, 620, charge);
        float halfAngle = Math.Max(.0005f, (1 - charge) * MathHelper.Pi / 6);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        // Async Effect.Value can be null on first use. Finish loading and parameter setup
        // while the caller's batch is still active, before entering either custom pass.
        Effect spread = spreadShader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ChargedSniper/SpreadTelegraph", AssetRequestMode.ImmediateLoad).Value;
        Effect line = lineShader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ChargedSniper/PixelatedSightLine", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise = Texture("CertifiedCrustyNoise");
        spread.Parameters["centerOpacity"].SetValue(.22f);
        spread.Parameters["mainOpacity"].SetValue(.25f + charge * .45f);
        spread.Parameters["halfSpreadAngle"].SetValue(halfAngle);
        spread.Parameters["edgeColor"].SetValue(color.ToVector3());
        spread.Parameters["centerColor"].SetValue(color.ToVector3() * .5f);
        spread.Parameters["edgeBlendLength"].SetValue(.07f);
        spread.Parameters["edgeBlendStrength"].SetValue(8f);
        DrawEffectPass(spread, () => {
            Main.EntitySpriteDraw(pixel, muzzle - Main.screenPosition, null, Color.White, aim.ToRotation(),
                pixel.Size() / 2, new Vector2(radius * 2) / pixel.Size(), SpriteEffects.None);
        });

        line.Parameters["sampleTexture2"].SetValue(noise);
        line.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -.003f);
        line.Parameters["mainOpacity"].SetValue(.35f + charge * .65f);
        line.Parameters["Resolution"].SetValue(new Vector2(radius * .22f));
        line.Parameters["laserWidth"].SetValue(.0025f + MathF.Pow(charge, 5) * .003f);
        line.Parameters["laserLightStrenght"].SetValue(7f);
        line.Parameters["color"].SetValue(color.ToVector3());
        line.Parameters["darkerColor"].SetValue(color.ToVector3() * .15f);
        line.Parameters["bloomSize"].SetValue(.045f);
        line.Parameters["bloomMaxOpacity"].SetValue(.35f);
        line.Parameters["bloomFadeStrenght"].SetValue(7f);
        DrawEffectPass(line, () => {
            for (int side = -1; side <= 1; side += 2)
            {
                line.Parameters["laserAngle"].SetValue(-aim.ToRotation() + side * halfAngle);
                Main.EntitySpriteDraw(pixel, muzzle - Main.screenPosition, null, Color.White, 0,
                    pixel.Size() / 2, new Vector2(radius * 2) / pixel.Size(), SpriteEffects.None);
                if (charge >= 1) break;
            }
        });

        // The center preview shares the fired beam's range and terrain scan, but never deals damage.
        float preview = Math.Clamp((charge - .5f) / .3f, 0, 1);
        if (preview > 0)
        {
            using var pass = new ChargedOperatorVfx.Pass(true);
            float length = ScanLength(muzzle, aim, kind, skill);
            BeamLayer(muzzle, aim, length, .06f + .05f * preview, color * (.2f + .25f * preview));
        }
        using (var pass = new ChargedOperatorVfx.Pass(true))
        {
            Ring(muzzle, aim.ToRotation(), new Vector2(8, 20) * (1.5f - charge * .6f), color * (.3f + charge * .4f));
        }
    }

    private static void DrawEffectPass(Effect effect, Action draw)
    {
        Main.spriteBatch.End();
        bool begun = false;
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            begun = true;
            draw();
        }
        finally
        {
            try { if (begun) Main.spriteBatch.End(); }
            finally
            {
                // Even an exception during Begin/drawing/End must return an active vanilla batch.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    }

    // Same four-layer composition as AMRBeamRound: glow, colored body, dark core, bright inner filament.
    internal static void DrawBeam(Vector2 start, Vector2 aim, float length, float scale, Color color, float remaining)
    {
        if (length <= 1 || scale <= .01f) return;
        using (var pass = new ChargedOperatorVfx.Pass(true))
        {
            BeamLayer(start, aim, length, scale * 1.35f, color * .65f);
            BeamLayer(start, aim, length, scale, Color.Lerp(color, Color.White, .3f) * .95f);
        }
        using (var pass = new ChargedOperatorVfx.Pass(false))
            BeamLayer(start, aim, length, scale * .48f, new Color(15, 13, 20) * .95f);
        using (var pass = new ChargedOperatorVfx.Pass(true))
        {
            BeamLayer(start, aim, length, scale * .22f, Color.Lerp(color, Color.White, .8f) * .9f);
            float growth = 1 - MathF.Pow(remaining, 4);
            for (float d = 0; d < length; d += 150)
                Ring(start + aim * d, aim.ToRotation(), new Vector2(12, 42) * (.3f + growth), color * (remaining * .65f));
        }
    }

    // UltimaRay textures point down. Crop the final middle segment to keep the endpoint at the collision boundary.
    private static void BeamLayer(Vector2 start, Vector2 aim, float length, float scale, Color color)
    {
        if (length <= 1 || scale <= .01f) return;
        Texture2D begin = Texture("UltimaRayStart"), middle = Texture("UltimaRayMid"), end = Texture("UltimaRayEnd");
        float angle = aim.ToRotation() - MathHelper.PiOver2;
        float cap = Math.Min(length / 2, begin.Height * scale * .5f);
        float tail = Math.Min(length / 2, end.Height * scale);
        Main.EntitySpriteDraw(begin, start - Main.screenPosition, null, color, angle,
            new Vector2(begin.Width / 2f, 0), new Vector2(scale, cap / begin.Height), SpriteEffects.None);
        float bodyEnd = length - tail;
        for (float d = cap; d < bodyEnd;)
        {
            float segment = Math.Min(middle.Height * scale, bodyEnd - d);
            int rows = Math.Clamp((int)MathF.Ceiling(segment / scale), 1, middle.Height);
            Main.EntitySpriteDraw(middle, start + aim * d - Main.screenPosition, new Rectangle(0, 0, middle.Width, rows), color,
                angle, new Vector2(middle.Width / 2f, 0), new Vector2(scale, segment / rows), SpriteEffects.None);
            d += segment;
        }
        Main.EntitySpriteDraw(end, start + aim * bodyEnd - Main.screenPosition, null, color, angle,
            new Vector2(end.Width / 2f, 0), new Vector2(scale, tail / end.Height), SpriteEffects.None);
    }
    private static void Ring(Vector2 center, float angle, Vector2 size, Color color)
    {
        Texture2D texture = Texture("HollowCircleHardEdge");
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, angle, texture.Size() / 2, size / texture.Size(), SpriteEffects.None);
    }

    internal static void DrawMuzzle(Vector2 center, Vector2 aim, Color color, bool heavy, float age)
    {
        float fade = Math.Clamp(1 - (age - 1) / 9f, 0, 1);
        if (fade <= 0) return;
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("StarCore", center, new Vector2(heavy ? 92 : 42, heavy ? 32 : 16) * fade,
            Color.Lerp(color, Color.White, .4f) * fade, aim.ToRotation());
        Ring(center + aim * age * 1.4f, aim.ToRotation(), new Vector2(12, 36) * (1 + age * .12f), color * fade);
    }
    internal static void MuzzleBurst(Vector2 center, Vector2 aim, ChargedOperatorKind kind, bool heavy)
    {
        if (!ChargedOperatorVfx.Visible(center)) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        for (int i = 0; i < (heavy ? 12 : 5); i++)
            ChargedOperatorVfx.Sparkle(center, aim.RotatedByRandom(.5f) * Main.rand.NextFloat(2, heavy ? 12 : 7),
                color, aim.ToRotation(), heavy ? 1 : .55f);
    }
    internal static void HitBurst(Vector2 center, Vector2 aim, ChargedOperatorKind kind, bool heavy, bool critical)
    {
        // Gameplay hooks only enqueue particles. All drawing stays in Particle.Draw.
        if (!ChargedOperatorVfx.Visible(center) || ParticleManager.activeParticles.Count >= 1000) return;
        float seed = Main.rand.NextFloat(MathHelper.TwoPi);
        new T_Sniper_Breach_Particle(center, aim, kind, heavy, critical, false, seed).Spawn();
        new T_Sniper_Breach_Flash(center, aim, ChargedOperatorRules.ColorFor(kind), heavy).Spawn();
        Color tint = ChargedOperatorRules.ColorFor(kind);
        int grains = heavy ? 22 : 10;
        for (int i = 0; i < grains; i++)
        {
            Vector2 velocity = aim.RotatedByRandom(i % 3 == 0 ? 2.2f : .85f) * Main.rand.NextFloat(3, heavy ? 17 : 10);
            new T_Sniper_Impact_Grain(center, velocity, tint, Main.rand.NextFloat(9, heavy ? 23 : 15),
                Main.rand.Next(18, 35), (i % 2 == 0 ? 1 : -1) * .035f).Spawn();
        }
        for (int i = 0; i < (heavy ? 8 : 4); i++)
            new T_Sniper_Impact_Cloud(center + Main.rand.NextVector2Circular(10, 10),
                aim.RotatedByRandom(1.4f) * Main.rand.NextFloat(1, 6), Color.Lerp(tint, new Color(25, 22, 35), .65f),
                Main.rand.NextFloat(45, heavy ? 100 : 65), Main.rand.Next(24, 42), seed + i).Spawn();
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sounds/ChargedSniper/" + (heavy ? "BreachHeavy" : "BreachLight"))
            { Volume = heavy ? .5f : .26f, Pitch = heavy ? -.2f : .15f, PitchVariance = .12f, MaxInstances = 3 }, center);
    }
    internal static void BeamParticles(Vector2 start, Vector2 aim, float length, ChargedOperatorKind kind)
    {
        if (Main.dedServ) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        for (float d = 0; d < length; d += 48)
        {
            Vector2 position = start + aim * d + aim.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-12, 12);
            if (!ChargedOperatorVfx.Visible(position)) continue;
            ChargedOperatorVfx.Sparkle(position, aim * Main.rand.NextFloat(3, 12), color, aim.ToRotation(), .7f);
            Lighting.AddLight(position, color.ToVector3() * .5f);
        }
    }

    internal static void BulletParticles(Projectile p, ChargedOperatorKind kind)
    {
        if (!ChargedOperatorVfx.Visible(p.Center)) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        if (kind == ChargedOperatorKind.Andreana)
        {
            Dust ink = Dust.NewDustPerfect(p.Center, DustID.TintableDustLighted, -p.velocity * .08f, 130, color * .45f, .7f);
            ink.noGravity = true;
        }
        else if (p.timeLeft % 8 == 0)
            ChargedOperatorVfx.Sparkle(p.Center, -p.velocity * .02f, color, p.rotation + (kind == ChargedOperatorKind.Firewatch ? .6f : 0), .45f);
    }
    internal static void DrawBullet(Projectile p, ChargedOperatorKind kind)
    {
        Color color = ChargedOperatorRules.ColorFor(kind);
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX), normal = new(-aim.Y, aim.X);
        if (kind == ChargedOperatorKind.Andreana)
        {
            using var ink = new ChargedOperatorVfx.Pass(false);
            ChargedOperatorVfx.Line(p.Center - aim * 60, p.Center, new Color(23, 17, 35) * .9f, 10);
        }
        using var pass = new ChargedOperatorVfx.Pass(true);
        Vector2 previous = p.Center;
        float traveled = 0;
        for (int i = 0; i < p.oldPos.Length; i++)
        {
            if (p.oldPos[i] == Vector2.Zero) break;
            Vector2 next = p.oldPos[i] + p.Size / 2;
            float segment = Vector2.Distance(previous, next);
            traveled += segment;
            if (segment > 100 || traveled > 240) break;
            float fade = 1 - traveled / 240;
            if (kind == ChargedOperatorKind.Ambriel)
            {
                ChargedOperatorVfx.Line(previous, next, color * fade * .65f, 2);
                if (i % 5 == 0) Ring(next, p.rotation, new Vector2(5, 13) * fade, color * fade * .6f);
            }
            else if (kind == ChargedOperatorKind.Andreana)
            {
                float curl = MathF.Sin(traveled * .08f + p.timeLeft * .12f) * 5 * fade;
                ChargedOperatorVfx.Line(previous + normal * curl, next + normal * curl, color * fade * .7f, 3.5f * fade);
            }
            else if (kind == ChargedOperatorKind.Firewatch)
            {
                if (i % 3 != 0)
                    foreach (int side in new[] { -1, 1 })
                        ChargedOperatorVfx.Line(previous + normal * side * 2, next + normal * side * 2, color * fade * .65f, 1.4f);
            }
            else
            {
                ChargedOperatorVfx.Line(previous, next, color * fade * .5f, 2);
                if (i % 4 == 0)
                    foreach (int side in new[] { -1, 1 })
                        ChargedOperatorVfx.Line(next, next - aim * 12 + normal * side * 7 * fade, color * fade * .8f, 1.5f);
            }
            previous = next;
        }
        ChargedOperatorVfx.Line(p.Center - aim * 24, p.Center + aim * 5, color, 4);
        ChargedOperatorVfx.Line(p.Center - aim * 13, p.Center + aim * 5, Color.White * .9f, 1.5f);
    }
}
