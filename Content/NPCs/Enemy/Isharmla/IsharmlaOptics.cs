using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaVisuals
{
    internal static Effect ParticleMaterial(int pass, Texture2D atlas)
    {
        EnsureEffect(); if (material == null) return null;
        material.Parameters["uImage"].SetValue(atlas); material.Parameters["uTime"].SetValue(Clock);
        material.Parameters["uStrength"].SetValue(1f);
        material.CurrentTechnique = material.Techniques[pass == 0 ? "ParticleBody" : pass == 1 ? "ParticleCrest" : "ParticleGlow"];
        return material;
    }
    public static void DrawField(Vector2 center, Vector2 size, float rotation, Color color, string technique, float strength = 1)
    {
        EnsureEffect(); if (material == null || color.A == 0) return;
        var batch = Main.spriteBatch; Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        batch.End();
        material.Parameters["uTime"].SetValue(Clock); material.Parameters["uStrength"].SetValue(strength);
        material.Parameters["uSize"].SetValue(size); material.Parameters["uDetail"].SetValue(Art("ParticleAtlasV2"));
        material.Parameters["uImage"].SetValue(pixel); material.CurrentTechnique = material.Techniques[technique];
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
        batch.Draw(pixel, center - Main.screenPosition, null, color, rotation, pixel.Size() * .5f, size / pixel.Size(), SpriteEffects.None, 0);
        Restore(batch);
    }
    private static void DrawSpriteRim(Texture2D texture, Vector2 screenPosition, Vector2 origin, float rotation, float scale, Color color,
        SpriteEffects effects = SpriteEffects.None)
    {
        EnsureEffect(); if (material == null) return;
        var batch = Main.spriteBatch; batch.End();
        material.Parameters["uTime"].SetValue(Clock); material.Parameters["uStrength"].SetValue(1f);
        material.Parameters["uImage"].SetValue(texture); material.Parameters["uTexel"].SetValue(new Vector2(1f / texture.Width, 1f / texture.Height));
        material.CurrentTechnique = material.Techniques["Outline"];
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
        batch.Draw(texture, screenPosition, null, color, rotation, origin, scale, effects, 0);
        Restore(batch);
    }
    public static void PortalField(Vector2 position, Vector2 direction, float radius, float progress, bool firing)
    {
        Color color = firing ? Color.Lerp(Cyan, Color.White, .35f) : Color.Lerp(Cyan, Gold, .2f);
        float angle = direction.ToRotation();
        DrawField(position, new Vector2(radius * .72f, radius * 2.4f), angle, color * .85f, "Portal", progress);
        DrawField(position, new Vector2(radius * 1.15f, radius * 2.9f), angle, Cyan * .22f, "Halo", progress);
    }
    public static void RayField(IsharmlaHazard h, Color color)
    {
        Vector2 axis = h.Direction.SafeNormalize(Vector2.UnitY), end = h.Projectile.Center + axis * h.Length;
        float progress = IsharmlaRules.Smooth(h.Age / (float)Math.Max(1, h.Delay));
        bool active = h.Age >= h.Delay;
        float fade = active ? Math.Min(1, Math.Max(0, Math.Min((h.Age - h.Delay + 1) / 6f, (h.Delay + h.Duration - h.Age) / 9f))) : 1;
        if (h.Kind == TideShot.Gate) PortalField(h.Projectile.Center, axis, h.Width * .85f, progress, active);
        else DrawField(h.Projectile.Center, new Vector2(h.Width * 3, h.Width * 3), Clock * .15f, color * (.6f * fade), "Halo", progress);
        if (!active)
        {
            Line(h.Projectile.Center, end, color * (.30f + progress * .3f), 2 + progress * 2);
            Line(h.Projectile.Center, end, color * .065f, h.Width);
            if (h.Kind == TideShot.Breath)
            {
                DrawField(h.Projectile.Center, new Vector2(135 + progress * 50), -Clock * .4f, color * .8f, "Sigil", progress);
                Orb(h.Projectile.Center, new Vector2(24 + progress * 22), Color.White * (.4f * progress));
            }
            return;
        }
        if (h.Kind == TideShot.Breath)
        {
            IsharmlaVapor.DrawRayVeil(Main.spriteBatch,h.Projectile.Center,axis,h.Length,h.Width,fade);
            DrawField((h.Projectile.Center + end) * .5f, new Vector2(h.Length, h.Width * 1.60f), axis.ToRotation(),
                Color.White * fade, "Disintegration");
            DrawField(h.Projectile.Center + axis * 28, new Vector2(180, 170), axis.ToRotation(), Color.White * fade, "PressureMuzzle");
            DrawField(end, new Vector2(95, h.Width * 1.65f), axis.ToRotation() + MathHelper.Pi, Color.White * fade, "PressureMuzzle");
            DrawField(h.Projectile.Center, new Vector2(70, 165), axis.ToRotation(), Azure * (.9f * fade), "Halo");
            return;
        }
        // Peripheral bloom is faint; the bright central channel follows the damaging ray exactly.
        DrawWaterMaterial(h.Projectile.Center, end, h.Width * 1.75f, color * (.23f * fade));
        DrawWaterMaterial(h.Projectile.Center, end, h.Width * 1.2f, color * (.95f * fade));
        DrawWaterMaterial(h.Projectile.Center + axis.RotatedBy(MathHelper.PiOver2) * 2, end,
            h.Width * .38f, Color.White * (.65f * fade));
        Line(h.Projectile.Center, end, Color.White * (.40f * fade), h.Width * .16f);
    }
    public static void OpticalBolt(Vector2 center, Vector2 direction, TideShot kind, float opacity = 1)
    {
        float rotation = direction.ToRotation();
        Vector2 size = IsharmlaRules.BoltSize(kind);
        Orb(center, size * 1.1f, Cyan * (.3f * opacity));
        DrawField(center, size, rotation + (kind == TideShot.Rain ? -MathHelper.PiOver2 : 0), Color.White * opacity, "Lens");
    }
    public static void HazardParticles(IsharmlaHazard h)
    {
        if (Main.dedServ) return;
        Vector2 axis = h.Direction.SafeNormalize(Vector2.UnitY), normal = axis.RotatedBy(MathHelper.PiOver2);
        bool ray = h.Kind is TideShot.Column or TideShot.Breath or TideShot.Gate or TideShot.Lattice;
        if (h.Age < h.Delay && ray)
        {
            if (h.Age % 3 == 0)
            {
                Vector2 edge = Main.rand.NextVector2CircularEdge(h.Kind == TideShot.Breath ? 100 : 65, 65);
                TidalParticles.Emit(TideParticle.Prism, h.Projectile.Center + edge, -edge * .055f,
                    new Vector2(5), h.Kind == TideShot.Gate ? Gold : Cyan, 22);
            }
            return;
        }
        if (h.Age == h.Delay)
        {
            if (ray)
            {
                TidalParticles.Emit(TideParticle.Ripple, h.Projectile.Center, Vector2.Zero, new Vector2(h.Width * .8f), Cyan, 40, impact: true);
                for (int i = 0; i < (h.Kind == TideShot.Breath ? 30 : 12); i++)
                    TidalParticles.Emit(i % 3 == 0 ? TideParticle.Prism : TideParticle.Foam, h.Projectile.Center,
                        axis * Main.rand.NextFloat(12, 32) + normal * Main.rand.NextFloat(-9, 9),
                        new Vector2(Main.rand.NextFloat(5, 11)), i % 3 == 0 ? Crest : Cyan, Main.rand.Next(18, 35), impact: true);
                // Native Dust is a sparse secondary layer; custom particles carry the composition.
                if (!ClockOverride.HasValue)
                    for (int i = 0; i < 8; i++)
                    {
                        Dust dust = Dust.NewDustPerfect(h.Projectile.Center, DustID.Water, axis * Main.rand.NextFloat(2, 7) + normal * Main.rand.NextFloat(-3, 3),
                            100, Cyan, Main.rand.NextFloat(.8f, 1.3f)); dust.noGravity = false;
                    }
            }
        }
        if (ray && h.Age >= h.Delay && h.Age % 3 == 0)
        {
            Vector2 point = h.Projectile.Center + axis * Main.rand.NextFloat(Math.Min(80, h.Length), h.Length);
            TidalParticles.Emit(TideParticle.Foam, point + normal * h.Width * Main.rand.NextFloat(-.4f, .4f), axis * 12 + normal * Main.rand.NextFloat(-2.5f, 2.5f),
                new Vector2(6, 10), Cyan, 24);
            if (h.Age % 6 == 0) TidalParticles.Emit(TideParticle.Mist, point, normal * Main.rand.NextFloat(-.8f, .8f),
                new Vector2(24, 12), Cyan, 42, axis.ToRotation());
            if (h.Kind == TideShot.Breath)
            {
                TidalParticles.Emit(TideParticle.Prism, h.Projectile.Center + normal * Main.rand.NextFloat(-36, 36),
                    axis * Main.rand.NextFloat(18, 42) + normal * Main.rand.NextFloat(-5, 5), new Vector2(5, 10), Crest, 20);
                if (h.Age % 6 == 0) TidalParticles.Emit(TideParticle.Lumen, h.Projectile.Center + axis * Main.rand.NextFloat(80, 900),
                    axis * 30, new Vector2(20, 5), Cyan, 14, axis.ToRotation());
            }
        }
        else if (h.Kind == TideShot.Wave && h.Age % 2 == 0)
        {
            TidalParticles.Emit(TideParticle.Foam, h.Projectile.Center - new Vector2(0, 65), new Vector2(-h.Direction.X * .1f, -1.2f), new Vector2(8, 5), Color.White, 45);
            if (h.Age % 6 == 0) TidalParticles.Emit(TideParticle.Wake, h.Projectile.Center, new Vector2(-h.Direction.X * .13f, 0), new Vector2(40, 12), Cyan, 55);
        }
        else if (h.Kind is TideShot.Pearl or TideShot.Seeker && h.Age % 5 == 0)
            TidalParticles.Emit(TideParticle.Bubble, h.Projectile.Center, -axis * .35f, new Vector2(4), Cyan, 40);
        else if (h.Kind == TideShot.RainCloud && h.Age % 9 == 0)
            TidalParticles.Emit(TideParticle.Mist, h.Projectile.Center + Main.rand.NextVector2Circular(h.Width * .4f, 12), new Vector2(.3f, -.15f),
                new Vector2(28, 15), Cyan, 55);
        else if (h.Kind is TideShot.Note or TideShot.CharmNote && h.Age >= h.Delay && h.Age % 7 == 0)
            TidalParticles.Emit(TideParticle.Note, h.Projectile.Center, -axis * .6f, new Vector2(8, 11),
                h.Kind == TideShot.CharmNote ? new Color(135, 65, 250) : Cyan, 20);
        else if (h.Kind == TideShot.StormSpark && h.Age >= h.Delay && h.Age % 5 == 0)
            TidalParticles.Emit(TideParticle.Prism, h.Projectile.Center, -axis, new Vector2(5), Crest, 16);
        else if (h.Kind == TideShot.MistField && h.Age % 8 == 0)
            TidalParticles.Emit(TideParticle.Mist, h.Projectile.Center + Main.rand.NextVector2Circular(h.Width * .65f, h.Width * .65f),
                new Vector2(.3f, -.3f), new Vector2(22), Azure, 28);
    }
    public static void DragonWake(Vector2 center, Vector2 velocity, float size, int seed, bool bright)
    {
        if (Main.dedServ || velocity.LengthSquared() < 4) return;
        Vector2 axis = velocity.SafeNormalize(Vector2.UnitX), normal = axis.RotatedBy(MathHelper.PiOver2);
        if ((int)(Clock * 60 + seed * 11) % 4 != 0) return;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 position = center + normal * side * size;
            TidalParticles.Emit(TideParticle.Wake, position, -axis * 1.4f, new Vector2(24, 5), bright ? Gold : Cyan, 34, axis.ToRotation());
            TidalParticles.Emit(TideParticle.Bubble, position + Main.rand.NextVector2Circular(8, 8), -axis * .7f - Vector2.UnitY * .4f,
                new Vector2(Main.rand.NextFloat(3, 6)), Cyan, 55);
        }
    }
}
