using System;
using ArknightsMod.Content.Projectiles.BossArsenal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

internal static class HarbingerVisuals
{
    internal static readonly Color Red = new(255, 38, 65), Cyan = new(15, 225, 220);
    internal static void Line(SpriteBatch batch, Vector2 a, Vector2 b, Color color, float width = 1)
    {
        Vector2 d = b - a;
        batch.Draw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), color, d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    internal static void Ring(SpriteBatch b, Vector2 center, float radius, Color color, int segments = 48, float yScale = 1, float rotate = 0)
    {
        for (int i = 0; i < segments; i++) {
            if (i % 8 == 0) continue;
            Vector2 a = (i * MathHelper.TwoPi / segments + rotate).ToRotationVector2() * radius;
            Vector2 c = ((i + 1) * MathHelper.TwoPi / segments + rotate).ToRotationVector2() * radius;
            Line(b, center + a * new Vector2(1, yScale), center + c * new Vector2(1, yScale), color, 2);
        }
    }
    internal static void DrawBoss(AACT boss, SpriteBatch b, Vector2 screen)
    {
        NPC n = boss.NPC;
        float death = boss.Phase == 4 ? Math.Clamp(boss.Clock / 180f, 0, 1) : 0;
        Vector2 center = n.Center - screen + new Vector2(0, 3);
        Texture2D Native(string name) => ModContent.Request<Texture2D>(boss.Texture[..^4] + name,
            ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        void Layer(string name, float opacity = 1, float rotation = 0, float scale = 1) {
            Texture2D texture = Native(name);
            b.Draw(texture, center, null, Color.White * opacity, rotation, texture.Size() / 2, scale, SpriteEffects.None, 0);
        }
        // The original dot field, rotating scanner, shade and outer frame are all retained.
        // In particular, this is the actual AARD art, not a new outline approximation.
        float ringOpacity = 1 - death * .8f;
        Layer("AARD", ringOpacity);
        DrawNativeScan(b, Native("AACTshadespin"), center - new Vector2(0, 3), Main.GlobalTimeWrappedHourly * MathHelper.Pi, ringOpacity);
        Layer("AACTshadering", ringOpacity);
        Layer("AARL", ringOpacity);
        Texture2D body = Native("AACT");
        if (death == 0) {
            ReadOnlySpan<Vector2> history = boss.NativeHistory;
            for (int i = history.Length - 1; i > 0; i--) if (i % 3 == 0 && history[i] != Vector2.Zero
                && Vector2.DistanceSquared(history[i], n.Center) < 600 * 600)
                b.Draw(body, history[i] - screen, null, Color.White * (1 - .025f * i), 0, body.Size() / 2, 1, SpriteEffects.None, 0);
        }
        float bodyScale = n.ai[3] > 0 ? 1 - MathF.Sin(Math.Clamp((110 - n.ai[3]) / 110f, 0, 1) * MathHelper.Pi) * .35f : 1;
        b.Draw(body, center, null, Color.White * (1 - death * .6f), n.rotation, body.Size() / 2, bodyScale, SpriteEffects.None, 0);
        Layer(n.dontTakeDamage && boss.Phase < 3 ? "AACTGlow" : "AACTGlow2", 1 - death, n.rotation, bodyScale);
        if (boss.Phase >= 2) {
            Layer("AACTEye", 1 - death, scale: bodyScale);
            Layer("AACTEye2", 1 - death, scale: bodyScale);
        }
    }
    private static void DrawNativeScan(SpriteBatch batch, Texture2D texture, Vector2 center, float angle, float opacity)
    {
        // Original mapping: rotate the 720px square, then compress world Y by 1.333.
        // Narrow texture columns retain that affine projection within the caller's batch.
        // This also preserves camera/target transforms used by other mods.
        Vector2 x = new(MathF.Cos(angle), MathF.Sin(angle) / 1.333f);
        Vector2 y = new(-MathF.Sin(angle), MathF.Cos(angle) / 1.333f);
        float length = y.Length(), rotation = y.ToRotation() - MathHelper.PiOver2;
        Vector2 scale = new(720f / texture.Width / (1.333f * length), 720f / texture.Height * length);
        for (int column = 0; column < texture.Width; column += 4) {
            int width = Math.Min(4, texture.Width - column);
            Vector2 at = center + x * ((column + width * .5f) / texture.Width * 720 - 360);
            batch.Draw(texture, at, new Rectangle(column, 0, width, texture.Height), Color.White * opacity,
                rotation, new Vector2(width * .5f, texture.Height * .5f), scale, SpriteEffects.None, 0);
        }
    }
    internal static void DrawHazard(HarbingerHazard h)
    {
        Projectile p = h.Projectile;
        SpriteBatch b = Main.spriteBatch;
        Vector2 at = p.Center - Main.screenPosition;
        int age = h.Age;
        if (h.Kind == 0) {
            float fade = Math.Clamp((100 - age) / 22f, 0, 1);
            Ring(b, at, p.ai[1], Red * fade, 48, 1, age * .018f);
            Ring(b, at, p.ai[1] * Math.Clamp((78 - age) / 78f, 0, 1), Cyan * fade, 32);
            for (int i = 0; i < 4; i++) {
                Vector2 axis = (i * MathHelper.PiOver2).ToRotationVector2();
                Line(b, at + axis * (p.ai[1] + 8), at + axis * (p.ai[1] + 23), Red * fade, 3);
            }
            if (age < 78) {
                Vector2 missile = at - Vector2.UnitY * ((78 - age) * 16);
                Line(b, missile - Vector2.UnitY * 75, missile, new Color(255, 170, 70) * .8f, 6);
            }
        }
        else if (h.Kind == 2) {
            SalvoExplosionVisuals.Draw(p.Center, age / 36f, p.ai[1], p.identity);
        }
        else if (h.Kind == 4) {
            Vector2 end = at + p.velocity * p.ai[1];
            float fade = MathF.Pow(Math.Max(0, 1 - age / 20f), 2);
            Line(b, at, end, Red * (.17f * fade), 32);
            Line(b, at, end, Red * (.6f * fade), 9);
            Line(b, at, end, Color.White * fade, 2);
            Ring(b, end, 20 + age * 4, Red * fade, 32);
        }
        else if (h.Kind == 6) {
            Ring(b, at, 48, Red * (.4f + MathF.Sin(age * .6f) * .2f), 32);
            Line(b, at - Vector2.UnitX * 7, at + Vector2.UnitX * 7, new Color(255, 140, 60), 5);
        }
        else {
            Color tint = h.Kind == 5 ? Red : new Color(255, 155, 65);
            for (int i = 1; i < p.oldPos.Length; i++) {
                if (p.oldPos[i] == Vector2.Zero || p.oldPos[i - 1] == Vector2.Zero) continue;
                Line(b, p.oldPos[i] + p.Size / 2 - Main.screenPosition, p.oldPos[i - 1] + p.Size / 2 - Main.screenPosition,
                    tint * ((1 - i / (float)p.oldPos.Length) * .65f), h.Kind == 5 ? 3 : 6);
            }
            Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitY);
            Line(b, at - axis * 16, at + axis * 12, tint, h.Kind == 5 ? 5 : 10);
            Line(b, at - axis * 8, at + axis * 10, Color.White, 3);
        }
    }
}
