using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionVisuals
{
    internal const string Root = "ArknightsMod/Content/NPCs/Enemy/Evolution/Assets/";
    internal static readonly Color Blood = new(222, 24, 53);
    internal static readonly Color Core = new(255, 196, 155);
    // Assets belong to tModLoader; clear references on unload, never dispose engine-owned textures.
    private static readonly Dictionary<string, Texture2D> textures = new();
    internal static Texture2D Asset(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D texture))
            textures[name] = texture = ModContent.Request<Texture2D>(name switch {
                "Bloom" => "ArknightsMod/Common/Particle/DefaultParticle",
                "CinematicRing" => "ArknightsMod/Content/Textures/circle_03",
                _ => Root + name
            }).Value;
        return texture;
    }
    internal static void Unload() => textures.Clear();
    internal static void AimLine(Vector2 start, Vector2 end, float progress)
    {
        float pulse = .4f + .6f * MathF.Abs(MathF.Sin(progress * MathHelper.Pi * 2));
        Line(start, end, Blood * (.75f * pulse), 2);
        Line(start, end, Core * (.65f * pulse), .8f, false);
    }
    internal static void DrawEvolved(SpriteBatch batch, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects flip)
    {
        // Both layers share the red core pivot. Draw the head last: never behind the tendrils.
        Texture2D body = Asset("EvolvedBody"), head = Asset("EvolvedHead");
        Vector2 bodyOrigin = new(56, 98), headOrigin = new(135, 102);
        if (flip != SpriteEffects.None) { bodyOrigin.X = body.Width - bodyOrigin.X; headOrigin.X = head.Width - headOrigin.X; }
        batch.Draw(body, position, null, color, rotation, bodyOrigin, scale, flip, 0);
        batch.Draw(head, position, null, color, rotation, headOrigin, scale, flip, 0);
    }

    // Premultiplied-alpha glow: no SpriteBatch state changes, no dependency on another mod's renderer.
    internal static void Glow(Vector2 center, Color color, Vector2 size, float rotation = 0f)
    {
        Texture2D texture = Asset("Bloom");
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color with { A = 0 }, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0f);
    }

    internal static void Line(Vector2 from, Vector2 to, Color color, float width, bool bloom = true)
    {
        Vector2 delta = to - from;
        if (delta.LengthSquared() < .01f) return;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1),
            color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
        if (bloom) Glow((from + to) * .5f, color * .4f, new Vector2(delta.Length() + width, width * 4f), delta.ToRotation());
    }

    internal static void Ring(Vector2 center, float radius, float gap, Color color, float width, bool gaps = true)
    {
        for (int i = 0; i < 120; i++)
        {
            float a = i * MathHelper.TwoPi / 120f;
            if (gaps && EvolutionRules.InGap(a + MathHelper.Pi / 120f, gap)) continue;
            Vector2 start = center + a.ToRotationVector2() * radius;
            if (Math.Abs(start.X - Main.screenPosition.X - Main.screenWidth * .5f) > Main.screenWidth * .5f + 150 ||
                Math.Abs(start.Y - Main.screenPosition.Y - Main.screenHeight * .5f) > Main.screenHeight * .5f + 150) continue;
            Line(start, center + (a + MathHelper.TwoPi / 120).ToRotationVector2() * radius, color, width);
        }
    }

    internal static void Burst(Vector2 center, float strength = 1f, bool sound = true)
    {
        if (Main.dedServ) return;
        int count = Math.Min(30, (int)(14 * strength));
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1, 1) * Main.rand.NextFloat(2, 8) * strength;
            new DefaultParticle(center, velocity, 22, Main.rand.NextFloat(.35f, .7f), i % 4 == 0 ? Core : Blood, true)
            { Deformation = new Vector2(.45f, 1.8f) }.Spawn();
            if (i % 3 == 0) Dust.NewDustPerfect(center, DustID.Blood, velocity * .7f, 0, default, 1.4f);
        }
        if (sound) SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = .65f, Pitch = -.3f, MaxInstances = 3 }, center);
    }

    internal static void Tendril(Vector2 start, Vector2 end, float bend, float time, float width, float alpha = 1f)
    {
        Vector2 delta = end - start;
        Vector2 normal = delta.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        Vector2 previous = start;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            Vector2 point = Vector2.Lerp(start, end, t) + normal * MathF.Sin(t * MathHelper.Pi) *
                (bend + MathF.Sin(t * 9 - time * .08f) * 9);
            float w = Math.Max(2, width * (1 - t));
            Line(previous, point, new Color(48, 3, 13) * alpha, w + 5, false);
            Line(previous, point, Blood * (.85f * alpha), w, false);
            Line(previous - normal * w * .2f, point - normal * w * .2f, Core * (.5f * alpha), Math.Max(1, w * .17f), false);
            previous = point;
        }
    }

    internal static void Trail(Projectile p, float width, Color color)
    {
        Vector2 previous = p.Center;
        for (int i = 0; i < p.oldPos.Length; i++)
        {
            if (p.oldPos[i] == Vector2.Zero) continue;
            Vector2 next = p.oldPos[i] + p.Size * .5f;
            float strength = 1f - i / (float)p.oldPos.Length;
            Line(previous, next, new Color(59, 0, 15) * strength, width * strength + 3, false);
            Line(previous, next, color * (strength * .8f), width * strength, i % 3 == 0);
            previous = next;
        }
    }
}
