using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Sniper.Schwarz;

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
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) || delta.LengthSquared() < .001f || delta.LengthSquared() > 360f * 360f) return;
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
        CrossbowFlightVfx.Draw(projectile, kind);
    }

    internal static void DrawPulse(Projectile projectile)
    {
        CrossbowKind kind = (CrossbowKind)(int)projectile.ai[0];
        bool muzzle = projectile.ai[1] == 1f;
        if (kind == CrossbowKind.Schwarz)
        {
            SchwarzBoltVisuals.DrawMuzzle(projectile);
            return;
        }
        Color color = ColorFor(kind);
        float progress = 1f - projectile.timeLeft / 20f;
        float fade = (1f - progress) * (1f - progress);
        float radius = (muzzle ? 6f : 10f) + MathF.Sqrt(progress) * 30f;
        Vector2 center = projectile.Center;
        float rotation = projectile.velocity.ToRotation();
        Glow(center, new Vector2(radius * 3, radius * 1.8f), Light(color, fade * .55f), rotation);
        Ring(center, radius, rotation, Light(color, fade), 2.4f * fade + .3f, muzzle ? .55f : 1f);
        Ring(center, radius * .65f, -rotation, Light(Color.White, fade * .65f), 1f, muzzle ? .5f : 1f);
        int rays = 8;
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
