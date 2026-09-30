using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowVisuals
{
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);
    internal static Color ColorFor(CrossbowKind kind) => kind switch {
        CrossbowKind.Kroos => new Color(242, 166, 67),
        CrossbowKind.KroosAlter => new Color(73, 222, 174),
        CrossbowKind.Schwarz => new Color(168, 149, 197),
        _ => new Color(255, 142, 188) };
    private static Color Light(Color c, float opacity) => new Color(c.R, c.G, c.B, 0) * opacity;
    internal static void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 delta = b - a;
        if (delta.LengthSquared() < .001f) return;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, Pixel, color,
            delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None);
    }
    internal static void Glow(Vector2 center, Vector2 size, Color color, float rotation = 0f)
    {
        Texture2D texture = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
        Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, rotation,
            texture.Size() * .5f, size / texture.Size(), SpriteEffects.None);
    }
    internal static void Ring(Vector2 center, float radius, float rotation, Color color, float width, float squash = 1f)
    {
        const int count = 36;
        Vector2 previous = center + new Vector2(radius, 0).RotatedBy(rotation);
        for (int i = 1; i <= count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            Vector2 next = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * squash).RotatedBy(rotation);
            Line(previous, next, color, width);
            previous = next;
        }
    }

    internal static void SpawnPulse(IEntitySource source, Vector2 position, Vector2 direction, CrossbowKind kind, bool muzzle)
    {
        // Only the firing client creates the visual projectile; other clients receive its spawn.
        if (source is not EntitySource_Parent parent || parent.Entity is not Projectile shot
            || shot.owner != Main.myPlayer) return;
        Projectile.NewProjectile(source, position, direction, ModContent.ProjectileType<CrossbowPulse>(),
            0, 0f, shot.owner, (int)kind, muzzle ? 1f : 0f);
    }

    internal static void DrawMuzzle(Vector2 center, Vector2 aim, CrossbowKind kind, float strength)
    {
        Color color = ColorFor(kind);
        Glow(center, new Vector2(64, 32) * strength, Light(color, strength), aim.ToRotation());
        Line(center - aim * 6, center + aim * 24 * strength, Light(Color.White, strength), 2f);
    }

    internal static void DrawBolt(Projectile projectile, CrossbowKind kind, int age)
    {
        bool black = kind == CrossbowKind.Schwarz;
        Color color = ColorFor(kind);
        Vector2 aim = projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 side = aim.RotatedBy(MathHelper.PiOver2);
        float power = black ? 1.6f : kind == CrossbowKind.Pozemka ? 1.25f : 1f;
        // Three-layer ribbon, twin bright edges and drifting sparks, built from the Keen-Glint arrow's silhouette.
        for (int i = projectile.oldPos.Length - 1; i > 0; i--)
        {
            if (projectile.oldPos[i] == Vector2.Zero || projectile.oldPos[i - 1] == Vector2.Zero) continue;
            Vector2 a = projectile.oldPos[i] + projectile.Size * .5f;
            Vector2 b = projectile.oldPos[i - 1] + projectile.Size * .5f;
            float fade = 1f - i / (float)projectile.oldPos.Length;
            float width = (black ? 15f : 7f) * fade;
            Line(a, b, Light(color, fade * .16f), width * 2.4f);
            Line(a, b, black ? new Color(5, 3, 10) * (fade * .95f) : Light(color, fade * .55f), width);
            Line(a + side * width * .6f, b + side * width * .6f, Light(color, fade * .8f), 1.2f * fade);
            Line(a - side * width * .6f, b - side * width * .6f, Light(Color.White, fade * .65f), .9f * fade);
            if (i % 4 == 0)
            {
                float wave = MathF.Sin(age * .24f - i * .8f + projectile.identity);
                Glow(a + side * wave * width, new Vector2(7, 3) * power, Light(color, fade * .7f), projectile.rotation);
            }
        }
        if (black) DrawBlackShader(projectile);

        Glow(projectile.Center, new Vector2(46, 19) * power, Light(color, .6f), aim.ToRotation());
        Texture2D arrow = TextureAssets.Projectile[projectile.type].Value;
        Main.EntitySpriteDraw(arrow, projectile.Center - Main.screenPosition, null,
            black ? new Color(12, 8, 20) : Color.Lerp(color, Color.White, .55f), projectile.rotation,
            arrow.Size() * .5f, black ? 1.3f : 1f, SpriteEffects.None);
        // Bright edge tips keep black arrows readable against caves AND bright backgrounds.
        Line(projectile.Center - aim * 15, projectile.Center + aim * 17,
            black ? new Color(3, 2, 7) : Light(color, .8f), black ? 5f : 2f);
        Line(projectile.Center + aim * 17, projectile.Center + aim * 5 + side * 5, Light(Color.White, .85f), 1.3f);
        Line(projectile.Center + aim * 17, projectile.Center + aim * 5 - side * 5, Light(color, .95f), 1.3f);
        if (black)
        {
            Ring(projectile.Center - aim * 10, 10f + MathF.Sin(age * .3f) * 2f,
                aim.ToRotation(), Light(color, .65f), 1.2f, 1.5f);
            Glow(projectile.Center + aim * 17, new Vector2(16, 5), Light(Color.White, .9f), aim.ToRotation());
        }
    }

    private static void DrawBlackShader(Projectile projectile)
    {
        int count = 0;
        while (count < projectile.oldPos.Length && projectile.oldPos[count] != Vector2.Zero) count++;
        if (count < 3) return;
        var rotations = new float[count];
        var positions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            positions[i] = projectile.oldPos[i];
            rotations[i] = projectile.oldRot[i] - MathHelper.PiOver2;
        }
        // Local animated ribbon shader. No screen capture or render-target switching in projectile drawing.
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            var shader = GameShaders.Misc["FlameLash"];
            shader.UseImage0("Images/Extra_194");
            shader.UseColor(new Color(16, 10, 24));
            shader.UseSecondaryColor(new Color(103, 80, 128));
            shader.UseSaturation(0f);
            shader.UseOpacity(.95f);
            shader.Apply();
            var strip = new VertexStrip();
            strip.PrepareStrip(positions, rotations,
                progress => Color.Lerp(new Color(22, 13, 32), new Color(90, 73, 112), progress) * (1f - progress),
                progress => 18f * MathF.Pow(1f - progress, .7f), -Main.screenPosition + projectile.Size * .5f);
            strip.DrawTrail();
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal static void DrawPulse(Projectile projectile)
    {
        CrossbowKind kind = (CrossbowKind)(int)projectile.ai[0];
        bool muzzle = projectile.ai[1] == 1f;
        bool black = kind == CrossbowKind.Schwarz;
        Color color = ColorFor(kind);
        float progress = 1f - projectile.timeLeft / 20f;
        float fade = (1f - progress) * (1f - progress);
        float radius = (muzzle ? 6f : 10f) + MathF.Sqrt(progress) * (black ? 52f : 30f);
        bool impact = black && !muzzle;
        if (impact) radius *= 1.65f;
        Vector2 center = projectile.Center;
        float rotation = projectile.velocity.ToRotation();
        Glow(center, new Vector2(radius * 3, radius * 1.8f), Light(color, fade * .55f), rotation);
        if (black) Ring(center, radius * .84f, rotation, new Color(5, 3, 8) * fade, 6f, muzzle ? .55f : 1f);
        Ring(center, radius, rotation, Light(color, fade), 2.4f * fade + .3f, muzzle ? .55f : 1f);
        Ring(center, radius * .65f, -rotation, Light(Color.White, fade * .65f), 1f, muzzle ? .5f : 1f);
        if (impact)
        {
            Ring(center, radius * 1.2f, rotation, Light(color, fade * .75f), 3f * fade, .8f);
            Ring(center, radius * .45f, -rotation, new Color(4, 2, 9) * fade, 10f * fade);
            Glow(center, new Vector2(170f, 12f) * (1f - progress), Light(Color.White, fade), rotation);
            Glow(center, new Vector2(100f, 8f) * (1f - progress), Light(color, fade), rotation + MathHelper.PiOver2);
        }
        int rays = impact ? 28 : black ? 14 : 8;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * MathHelper.TwoPi / rays + projectile.identity * .8f;
            Vector2 dir = angle.ToRotationVector2();
            Vector2 a = center + dir * radius * .6f;
            Vector2 b = center + dir * radius * (1.1f + .25f * MathF.Sin(i * 5f));
            Line(a, b, Light(color, fade), 1.5f);
            Glow(b, new Vector2(7, 3), Light(Color.White, fade), angle);
        }
    }
}

public sealed class CrossbowPulse : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.timeLeft = 20;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Main.dedServ || Projectile.timeLeft != 20) return;
        Color color = CrossbowVisuals.ColorFor((CrossbowKind)(int)Projectile.ai[0]);
        bool impact = projectileImpact();
        for (int i = 0; i < (impact ? 28 : 10); i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                Main.rand.NextVector2CircularEdge(impact ? 8f : 4f, impact ? 8f : 4f) * Main.rand.NextFloat(.5f, 1.8f), 60, color, impact ? 1.4f : 1f);
            dust.noGravity = true;
        }
    }
    private bool projectileImpact() => (CrossbowKind)(int)Projectile.ai[0] == CrossbowKind.Schwarz && Projectile.ai[1] == 0f;
    public override bool PreDraw(ref Color lightColor) { CrossbowVisuals.DrawPulse(Projectile); return false; }
}
