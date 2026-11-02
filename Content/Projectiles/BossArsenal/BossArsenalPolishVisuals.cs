using System;
using System.Reflection;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class BossArsenalPolishVisuals
{
    // Vanilla's flame renderer explicitly slices Projectile_85 into seven rows,
    // layers warm cores and smoke, and treats ai[0]==1 as the Elf Melter palette.
    // A detached visual proxy avoids changing any gameplay AI or global assets.
    private static readonly Action<Projectile> DrawVanillaFlame = (Action<Projectile>)typeof(Main)
        .GetMethod("DrawProj_Flamethrower", BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate(typeof(Action<Projectile>));
    [ThreadStatic] private static Projectile flame;
    internal static void Flame(Projectile host, float age)
    {
        Main.instance.LoadProjectile(ProjectileID.Flames);
        flame ??= new Projectile(); flame.type = ProjectileID.Flames;
        flame.Center = host.Center; flame.velocity = host.velocity; flame.rotation = host.rotation;
        flame.localAI[0] = Math.Clamp(age * 72, 1, 71); flame.ai[0] = 0;
        DrawVanillaFlame(flame);
    }
    internal static void Missile(Projectile shot)
    {
        Main.instance.LoadProjectile(ProjectileID.RocketI); Texture2D tex = TextureAssets.Projectile[ProjectileID.RocketI].Value;
        Main.EntitySpriteDraw(tex, shot.Center - Main.screenPosition, null, Color.White, shot.velocity.ToRotation() + MathHelper.PiOver2,
            tex.Size() * .5f, 1.3f, SpriteEffects.None);
        // A solid visible body exists even before the imported engine ignites.
    }
    internal static void IceMissile(Projectile shot) => IceMissile(shot, Color.LightCyan);
    internal static void IceMissile(Projectile shot, Color color)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        Vector2 direction = shot.velocity.SafeNormalize(Vector2.UnitX);
        ChargedOperatorVfx.Line(shot.Center - direction * 14, shot.Center + direction * 5, color, 3);
        ChargedOperatorVfx.Sprite("LinearSpark", shot.Center, new Vector2(22, 10), color * .5f, shot.rotation);
    }
    internal static void Crown(Projectile shot)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        float clock = shot.ai[1], phase = clock % 180;
        float power = phase >= 60 && phase < 126 ? 1 : .65f;
        Vector2 at = shot.Center;
        for (int i = 0; i < 96; i++)
        {
            float angle = i * MathHelper.TwoPi / 96 + clock * .007f;
            Vector2 a = at + angle.ToRotationVector2() * 30, b = at + (angle + MathHelper.TwoPi / 96).ToRotationVector2() * 30;
            ChargedOperatorVfx.Line(a, b, Color.LightCyan * power, 1.7f);
        }
        for (int i = 0; i < 16; i++)
        {
            float angle = i * MathHelper.TwoPi / 16 - clock * .012f;
            Vector2 ray = angle.ToRotationVector2();
            float pulse = 3 + 2 * MathF.Sin(clock * .05f + i * MathHelper.Pi);
            ChargedOperatorVfx.Line(at + ray * (35 + pulse), at + ray * (43 + pulse), Color.Cyan * power, 2);
            Vector2 point = at + ray * 24;
            ChargedOperatorVfx.Line(point - ray.RotatedBy(.5f) * 4, point, Color.White * (.5f * power), 1);
        }
        ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(60), Color.Cyan * .24f * power);
        ChargedOperatorVfx.Sprite("LinearSpark", at, new Vector2(13, 26), Color.White * .6f * power, clock * .02f);
    }
    internal static void SnowBurst(Vector2 at)
    {
        for (int i = 0; i < 42; i++)
        {
            Vector2 ray = (i * 2.399963f).ToRotationVector2();
            new T_BossIceShard(at + ray * Main.rand.NextFloat(0, 20), ray * Main.rand.NextFloat(3, 13),
                Main.rand.NextFloat(4, 10), i % 3 == 0).Spawn();
        }
        for (int i = 0; i < 80; i++)
        {
            Vector2 ray = (i * 2.399963f).ToRotationVector2();
            var dust = Dust.NewDustPerfect(at + ray * Main.rand.NextFloat(0, 55), i % 2 == 0 ? DustID.IceTorch : DustID.Snow,
                ray * Main.rand.NextFloat(3, 15), 50, Color.LightCyan, Main.rand.NextFloat(1.2f, 2.3f));
            dust.noGravity = true;
        }
    }
    internal static void DrawSnowBurst(Vector2 at, float t)
    {
        using var pass = new ChargedOperatorVfx.Pass(true);
        float fade = 1 - t;
        ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(250, 130) * MathF.Sqrt(Math.Min(1, t * 3)), Color.LightCyan * fade);
        ChargedOperatorVfx.Sprite("RuptureRing", at, new Vector2(205, 230) * Math.Min(1, t * 4), Color.Cyan * (.6f * fade));
        for (int i = 0; i < 12; i++)
        {
            Vector2 ray = (i * MathHelper.TwoPi / 12 + .13f).ToRotationVector2();
            Vector2 from = at + ray * (10 + t * 30), tip = at + ray * (30 + MathF.Sqrt(t) * 125);
            ChargedOperatorVfx.Line(from, tip, Color.LightCyan * fade, (i % 2 == 0 ? 4 : 2) * fade);
            ChargedOperatorVfx.Line(tip, tip - ray.RotatedBy(.6f) * 13 * fade, Color.White * fade, 1.5f);
        }
        ChargedOperatorVfx.Sprite("LinearSpark", at, new Vector2(180, 25) * fade, Color.White * fade);
        ChargedOperatorVfx.Sprite("LinearSpark", at, new Vector2(110, 18) * fade, Color.LightCyan * fade, MathHelper.PiOver2);
    }
    internal static void Coffin(Projectile shot, int age)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        float scale = MathHelper.SmoothStep(.05f, 1, Math.Min(1, age / 12f));
        Vector2 at = shot.Center - Main.screenPosition;
        void Patch(Vector2 local, Vector2 size, Color color) => Main.spriteBatch.Draw(pixel, at + local * scale,
            new Rectangle(0, 0, 1, 1), color, 0, new Vector2(.5f), size * scale, SpriteEffects.None, 0);
        void Edge(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            Vector2 delta = (b - a) * scale;
            Main.spriteBatch.Draw(pixel, at + a * scale, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(),
                new Vector2(0, .5f), new Vector2(delta.Length(), width * scale), SpriteEffects.None, 0);
        }
        // Uniform scaling applies to front, side, top, facets and etchings alike.
        Patch(Vector2.Zero, new Vector2(80), new Color(5, 5, 12));
        for (int i = 0; i < 12; i++) Patch(new Vector2(40 + i, -i * .83f), new Vector2(1.2f, 80), new Color(7 + i / 3, 5 + i / 4, 17 + i / 2));
        for (int i = 0; i < 10; i++) Patch(new Vector2(12 - i * 1.2f, -50 + i), new Vector2(80, 1.2f), new Color(22 + i / 2, 18 + i / 2, 38 + i));
        for (int row = 0; row < 20; row++) for (int col = 0; col < 20; col++)
        {
            float vein = MathF.Sin(col * .62f + MathF.Sin(row * .45f) * 2 + row * .23f);
            int shade = (int)(5 + (vein + 1) * 5 + ((col * 17 + row * 31) % 7));
            Patch(new Vector2(-38 + col * 4, -38 + row * 4), new Vector2(4.1f), new Color(shade, shade - 2, shade + 10));
        }
        Color edge = new Color(145, 105, 190) * (.5f + .1f * MathF.Sin(age * .1f));
        Edge(new(-40, -40), new(40, -40), edge); Edge(new(-40, -40), new(-40, 40), edge * .6f);
        Edge(new(40, -40), new(40, 40), edge); Edge(new(40, 40), new(-40, 40), edge * .6f);
        Edge(new(-40, -40), new(-28, -50), edge); Edge(new(-28, -50), new(52, -50), edge);
        Edge(new(52, -50), new(52, 30), edge * .6f); Edge(new(52, 30), new(40, 40), edge);
        Edge(new(40, -40), new(52, -50), edge);
        for (int i = 0; i < 7; i++)
        {
            Vector2 start = new(-37, -31 + i * 10), bend = new(-7 + i * 3, -24 + i * 8), end = new(36, -30 + i * 10);
            Edge(start, bend, edge * .35f); Edge(bend, end, edge * .55f);
            Edge(bend, bend + new Vector2(4, -8), edge * .32f);
        }
        // Recessed double border and bright carved seal give the dark mass depth.
        Edge(new(-32, -33), new(31, -33), edge * .4f); Edge(new(-32, -33), new(-32, 32), edge * .3f);
        Edge(new(31, -33), new(31, 32), edge * .5f); Edge(new(-32, 32), new(31, 32), edge * .3f);
        Edge(new(0, -20), new(0, 22), new Color(190, 140, 235), 2);
        Edge(new(-12, -4), new(12, 4), edge, 2); Edge(new(-8, 15), new(8, -13), edge);
    }
    internal static void GroundFire(Projectile shot)
    {
        float fade = Math.Min(1, shot.timeLeft / 12f);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        for (int i = 0; i < 7; i++)
        {
            float h = 15 + 18 * (.5f + .5f * MathF.Sin(shot.localAI[0] * .22f + i * 2));
            Vector2 at = shot.Bottom + new Vector2((i - 3) * 3.5f, -h * .5f) - Main.screenPosition;
            Main.spriteBatch.Draw(pixel, at, new Rectangle(0, 0, 1, 1), new Color(4, 3, 11) * fade,
                MathF.Sin(i + shot.localAI[0] * .1f) * .16f, new Vector2(.5f), new Vector2(6, h), SpriteEffects.None, 0);
        }
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("LinearSpark", shot.Bottom - new Vector2(0, 15), new Vector2(8, 35), new Color(80, 45, 130) * (.5f * fade));
        ChargedOperatorVfx.Sprite("RuptureRing", shot.Bottom, new Vector2(35, 9), Color.MediumPurple * (.3f * fade));
    }
}
