using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    private static void DrawDeathChassis(MaterialistAntagonizer boss, Texture2D body, SpriteBatch batch,
        Vector2 screenPos, Color light)
    {
        int age = boss.Timer;
        float failure = MaterialistDeathRules.Smooth(24, 178, age);
        float opacity = 1 - MaterialistDeathRules.Smooth(148, MaterialistDeathRules.Duration, age);
        float split = MaterialistDeathRules.Smooth(96, MaterialistDeathRules.Finale, age);
        Vector2 position = boss.NPC.Center + new Vector2(MathF.Sin(age * 2.3f), MathF.Cos(age * 3.1f)) * (2 + failure * 5);
        Color tint = Color.Lerp(Cyan, new Color(255, 137, 70), failure);
        float engine = (1 - MaterialistDeathRules.Smooth(80, 170, age)) * opacity;
        foreach (float x in new[] { -85f, 78f })
            EngineFlame(position + new Vector2(x, -8).RotatedBy(boss.NPC.rotation) * 1.45f,
                boss.NPC.velocity, boss.NPC.rotation, false, tint, age, engine);
        // Real art separates into horizontal chassis plates; the original sprite is never replaced by boxes.
        for (int strip = 0; strip < 12; strip++)
        {
            int y = strip * body.Height / 12, end = (strip + 1) * body.Height / 12;
            Rectangle frame = new(0, y, body.Width, end - y);
            float side = strip % 2 == 0 ? -1 : 1;
            Vector2 offset = new(side * split * (14 + strip * 3),
                (y + frame.Height * .5f - body.Height * .5f) * 1.45f + split * (strip - 5.5f) * 4);
            Vector2 point = position + offset.RotatedBy(boss.NPC.rotation);
            if (!Reduced && failure > .4f)
                batch.Draw(body, point - screenPos + new Vector2(side * 3, 0), frame,
                    (tint with { A = 0 }) * (.35f * failure * opacity), boss.NPC.rotation,
                    frame.Size() * .5f, 1.45f, SpriteEffects.None, 0);
            batch.Draw(body, point - screenPos, frame, Color.Lerp(light, Color.White, .75f) * opacity,
                boss.NPC.rotation + side * split * .025f, frame.Size() * .5f, 1.45f, SpriteEffects.None, 0);
        }
        MaterialistTechRenderer.ChassisMaterial(body, position, null, boss.NPC.rotation, new Vector2(1.45f),
            SpriteEffects.None, tint * ((.2f + failure * .7f) * opacity), 5, failure, age / 60f);
        Glow(position + new Vector2(4, 31) * 1.45f, Color.Lerp(Cyan, White, failure) * (failure * opacity),
            new Vector2(28 + failure * 90, 28 + failure * 45));
        DrawTerminalCuts(boss.NPC.Center, age);
    }

    internal static void DrawDeathDisplay(Vector2 center, float rotation, int age)
    {
        if (age < MaterialistDeathRules.Finale) DrawFailingNetwork(center, age);
        DrawTerminalLasers(center, rotation, age);
        for (int birth = Math.Max(0, age - 26); birth <= Math.Min(age, MaterialistDeathRules.Duration - 1); birth++)
        {
            if (!MaterialistDeathRules.Burst(birth)) continue;
            float t = (age - birth) / 27f, fade = (1 - t) * (1 - t);
            for (int i = 0; i < MaterialistDeathRules.BurstCount(birth, Reduced); i++)
            {
                Vector2 point = MaterialistDeathRules.FaultPoint(center, birth, i);
                Color tint = MaterialistDeathRules.Tint(birth / 5 + i);
                float size = 16 + MathF.Sqrt(t) * (42 + birth * .30f);
                Glow(point, tint * (.65f * fade), new Vector2(size * 2, size));
                Glow(point, (Color.White with { A = 0 }) * (.7f * fade), new Vector2(size, size * .3f));
                DrawSquareWave(point, size, tint * (.65f * fade), birth * .017f, 2 + 2 * fade);
            }
        }
        if (age >= MaterialistDeathRules.Finale) DrawTerminalFinale(center, age - MaterialistDeathRules.Finale);
    }

    private static void DrawFailingNetwork(Vector2 center, int age)
    {
        float build = MaterialistDeathRules.Smooth(0, 36, age), charge = MaterialistDeathRules.Smooth(24, 176, age);
        Color tint = Color.Lerp(Cyan, new Color(255, 107, 62), charge);
        Vector2 size = new(420 + charge * 65, 230 + charge * 35);
        Brackets(center, size, tint, build * .7f);
        int rows = Reduced ? 4 : 11;
        for (int row = 0; row < rows; row++)
        {
            float y = (row / (float)(rows - 1) - .5f) * size.Y;
            float glitch = MathF.Sin(MathF.Floor(age / 3f) * 2.1f + row * 3.7f) * charge * 32;
            Vector2 point = center + new Vector2(glitch, y);
            Line(point - Vector2.UnitX * size.X * .5f, point + Vector2.UnitX * size.X * .5f,
                tint * (build * (.08f + charge * .09f)), 1);
        }
        float scan = age / 32f % 1;
        Glow(center + new Vector2(0, (scan - .5f) * size.Y), tint * (.35f * build), new Vector2(size.X, 12));
        // Bounded square packets pull in, stutter, then reverse as the command bus fails.
        int packets = MaterialistDeathRules.PacketCount(Reduced);
        for (int i = 0; i < packets; i++)
        {
            float time = (age * .011f + MaterialistDeathRules.Hash(i * 11)) % 1;
            float angle = MaterialistDeathRules.Hash(i * 19) * MathHelper.TwoPi;
            float radius = MathHelper.Lerp(90, 390, age < 110 ? 1 - time : time);
            Vector2 point = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .65f);
            float fade = MathF.Sin(time * MathHelper.Pi) * build * .55f;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, point - Main.screenPosition, null,
                (MaterialistDeathRules.Tint(i) with { A = 0 }) * fade, 0, new Vector2(.5f),
                new Vector2(2 + i % 4), SpriteEffects.None, 0);
        }
    }

    private static void DrawTerminalLasers(Vector2 center, float rotation, int age)
    {
        if (age < 48 || age > 222) return;
        Main.instance.LoadProjectile(ProjectileID.PhantasmalDeathray);
        Texture2D start = TextureAssets.Projectile[ProjectileID.PhantasmalDeathray].Value;
        Texture2D middle = TextureAssets.Extra[21].Value, end = TextureAssets.Extra[22].Value;
        // Native textured beam layers, not hostile beam entities; the whole batch shares one material pass.
        MaterialistTechRenderer.NativeLaser(() =>
        {
            for (int birth = 48; birth < MaterialistDeathRules.Finale; birth += 14)
            {
                int elapsed = age - birth;
                if (elapsed < 0 || elapsed >= 42) continue;
                int count = Reduced ? 1 : birth >= 156 ? 5 : birth >= 90 ? 3 : 2;
                float fade = 1 - MaterialistDeathRules.Smooth(24, 42, elapsed);
                float grow = MaterialistDeathRules.Smooth(6, 18, elapsed);
                for (int i = 0; i < count; i++)
                {
                    float angle = MaterialistDeathRules.Hash(birth * 13 + i * 7) * MathHelper.TwoPi + rotation;
                    Vector2 axis = angle.ToRotationVector2();
                    Vector2 origin = center + axis * (38 + i * 16);
                    float length = (620 + MaterialistDeathRules.Hash(birth + i) * 480) * grow;
                    float width = (12 + birth * .10f) * grow * fade;
                    Color tint = MaterialistDeathRules.Tint(i + birth / 14);
                    if (length < 1) continue;
                    if (!Reduced) DrawTerminalBeamLayer(start, middle, end, origin, axis, length, width * 2, tint * (.2f * fade), age);
                    DrawTerminalBeamLayer(start, middle, end, origin, axis, length, width, tint * (.9f * fade), age);
                    DrawTerminalBeamLayer(start, middle, end, origin, axis, length, width * .33f, Color.White * (.9f * fade), age);
                }
            }
        });
    }
    private static void DrawTerminalBeamLayer(Texture2D start, Texture2D middle, Texture2D end, Vector2 origin,
        Vector2 axis, float length, float width, Color tint, int age)
    {
        float angle = axis.ToRotation() - MathHelper.PiOver2;
        float cap = Math.Min(22, length * .2f), bodyEnd = length - cap;
        DrawBeamPiece(start, start.Bounds, origin, angle, width, cap, tint);
        for (float along = cap; along < bodyEnd; along += 32)
        {
            int frame = (age / 3 + (int)(along / 32)) % Math.Max(1, middle.Height / 16);
            DrawBeamPiece(middle, new Rectangle(0, frame * 16, middle.Width, 16), origin + axis * along,
                angle, width, Math.Min(32, bodyEnd - along), tint);
        }
        DrawBeamPiece(end, end.Bounds, origin + axis * bodyEnd, angle, width, cap, tint);
    }

    private static void DrawTerminalCuts(Vector2 center, int age)
    {
        for (int birth = 72; birth < MaterialistDeathRules.Finale; birth += Reduced ? 22 : 11)
        {
            int elapsed = age - birth;
            if (elapsed < 0 || elapsed >= 28) continue;
            float t = elapsed / 28f, fade = MathF.Sin(t * MathHelper.Pi);
            float angle = MaterialistDeathRules.Hash(birth * 31) * MathHelper.Pi;
            Vector2 axis = angle.ToRotationVector2(), normal = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 point = center + normal * MathHelper.Lerp(-95, 95, MaterialistDeathRules.Smooth(0, 20, elapsed));
            Color tint = MaterialistDeathRules.Tint(birth / 11);
            float length = 420 + birth * 2;
            if (!MaterialistTechRenderer.DeathCut(point, new Vector2(length, 42 + birth * .15f), angle, tint * fade, t, age / 60f))
            {
                Glow(point, tint * (.35f * fade), new Vector2(length, 32), angle);
                Line(point - axis * length * .5f, point + axis * length * .5f, (Color.White with { A = 0 }) * fade, 3);
            }
        }
    }

    private static void DrawTerminalFinale(Vector2 center, int elapsed)
    {
        float t = elapsed / (float)MaterialistDeathRules.Aftermath;
        float fade = MathF.Pow(Math.Max(0, 1 - t), 1.5f);
        if (fade <= 0) return;
        float expand = MathF.Sqrt(t);
        int layers = Reduced ? 2 : 4;
        for (int i = 0; i < layers; i++)
            DrawSquareWave(center, (95 + expand * 650) * (1 - i * .12f),
                MaterialistDeathRules.Tint(i) * (fade * .6f), MathHelper.PiOver4 + i * .06f, 2 + fade * 4);
        Glow(center, Cyan * (fade * .6f), new Vector2(300 + expand * 700, 170 + expand * 300));
        Glow(center, White * (fade * .8f), new Vector2(640 + expand * 300, 12 + fade * 42));
        Glow(center, Color.White * (fade * .6f), new Vector2(70 + expand * 150));
        int rays = Reduced ? 6 : 24;
        for (int i = 0; i < rays; i++)
        {
            Vector2 axis = (i * MathHelper.TwoPi / rays + .15f).ToRotationVector2();
            Vector2 point = center + axis * (80 + expand * 580);
            Glow(point, MaterialistDeathRules.Tint(i) * (.45f * fade), new Vector2(65 + fade * 140, 3 + fade * 8), axis.ToRotation());
        }
    }
    private static void DrawSquareWave(Vector2 point, float radius, Color tint, float angle, float width)
    {
        Vector2 a = new Vector2(-radius, -radius).RotatedBy(angle), b = new Vector2(radius, -radius).RotatedBy(angle);
        Vector2 c = -a, d = -b;
        Line(point + a, point + b, tint, width); Line(point + b, point + c, tint, width);
        Line(point + c, point + d, tint, width); Line(point + d, point + a, tint, width);
    }
}
