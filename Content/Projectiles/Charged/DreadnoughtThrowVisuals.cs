using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Charged;

internal static class DreadnoughtThrowVisuals
{
    private static readonly Dictionary<ChargedOperatorKind, Texture2D> bodies = new();
    private static readonly Dictionary<ChargedOperatorKind, Texture2D> outlines = new();
    internal static void Unload()
    {
        var owned = new List<Texture2D>(outlines.Values); outlines.Clear(); bodies.Clear();
        Main.QueueMainThreadAction(() => { foreach (var texture in owned) texture.Dispose(); });
    }
    internal static void Release(Vector2 at, Vector2 axis, ChargedOperatorKind kind)
    {
        if (!ChargedOperatorVfx.Visible(at)) return;
        SoundEngine.PlaySound(SoundID.Item1 with { Volume = .65f, Pitch = .1f + (int)kind * -.06f, MaxInstances = 4 }, at);
        Emit(at, axis * -1, kind, 5, 4);
    }
    internal static void Extract(Vector2 at, Vector2 axis, ChargedOperatorKind kind)
    {
        if (!ChargedOperatorVfx.Visible(at)) return;
        SoundEngine.PlaySound(SoundID.Item73 with { Volume = .4f, Pitch = .12f, MaxInstances = 5 }, at);
        Emit(at, axis, kind, 14, 8);
    }
    internal static void Emit(Vector2 at, Vector2 axis, ChargedOperatorKind kind, int count, float speed)
    {
        if (!ChargedOperatorVfx.Visible(at)) return;
        for (int i = 0; i < count && ParticleManager.activeParticles.Count < 1200; i++)
        {
            Vector2 direction = axis.RotatedByRandom(.9f);
            if (i % 4 == 0) direction *= -1;
            new DreadnoughtMoteParticle(at + Main.rand.NextVector2Circular(6, 6), direction * Main.rand.NextFloat(.3f, 1) * speed,
                kind, Main.rand.Next(15, 32), Main.rand.NextFloat(.7f, 1.4f)).Spawn();
        }
    }
    internal static void Flight(Projectile p, ChargedOperatorKind kind, bool returning, bool moving)
    {
        if (!ChargedOperatorVfx.Visible(p.Center)) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        Lighting.AddLight(p.Center, color.ToVector3() * (returning ? .7f : .35f));
        if (moving && Main.rand.NextBool(returning ? 1 : 2))
            Emit(p.Center, -p.velocity.SafeNormalize(Vector2.UnitX), kind, returning ? 2 : 1, returning ? 4 : 2);
    }
    private static Texture2D Body(ChargedOperatorKind kind)
    {
        if (bodies.TryGetValue(kind, out Texture2D cached)) return cached;
        if (kind == ChargedOperatorKind.Melantha)
            return bodies[kind] = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Guard/Melantha/MelanthasSword_white").Value;
        if (kind == ChargedOperatorKind.Matoimaru)
            return bodies[kind] = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Guard/Matoimaru/MataimaruGlaive").Value;
        int item = kind == ChargedOperatorKind.Franka ? ItemID.Cutlass : ItemID.DayBreak;
        Main.instance.LoadItem(item);
        return bodies[kind] = TextureAssets.Item[item].Value;
    }
    private static Texture2D Outline(ChargedOperatorKind kind, Texture2D body)
    {
        if (outlines.TryGetValue(kind, out Texture2D result)) return result;
        Color[] source = new Color[body.Width * body.Height]; body.GetData(source);
        int width = body.Width + 4, height = body.Height + 4;
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int sx = x - 2, sy = y - 2, outer = 0;
            int center = (uint)sx < body.Width && (uint)sy < body.Height ? source[sy * body.Width + sx].A : 0;
            for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
            {
                int nx = sx + dx, ny = sy + dy;
                if (dx * dx + dy * dy > 4 || (uint)nx >= body.Width || (uint)ny >= body.Height) continue;
                outer = Math.Max(outer, source[ny * body.Width + nx].A);
            }
            pixels[y * width + x] = Color.White * (Math.Max(0, outer - center) / 255f);
        }
        result = new Texture2D(Main.spriteBatch.GraphicsDevice, width, height); result.SetData(pixels);
        outlines[kind] = result; return result;
    }
    internal static void Draw(Projectile p, ChargedOperatorKind kind, bool returning, bool moving, float length, Color light)
    {
        if (!ChargedOperatorVfx.Visible(p.Center)) return;
        Color color = ChargedOperatorRules.ColorFor(kind);
        if (moving) DreadnoughtArcTrail.DrawFlight(p, kind, returning);
        Texture2D body = Body(kind), edge = Outline(kind, body);
        var layout = DreadnoughtBladeLayout.For(kind, body.Size());
        float angle = p.rotation - layout.Axis(false).ToRotation(), scale = length / layout.Axis(false).Length();
        Vector2 at = p.Center - Main.screenPosition;
        if (returning)
        {
            // SHPCKFast's two-layer tinted weapon silhouettes, with each
            // operator's palette replacing its cyan/blue cycle.
            Color pale = Color.Lerp(color, Color.White, .62f);
            Color deep = Color.Lerp(color, Color.Black, .42f);
            for (int i = 1; i < p.oldPos.Length; i += 3)
            {
                if (p.oldPos[i] == Vector2.Zero) continue;
                float phase = MathF.Cos(Main.GlobalTimeWrappedHourly * 3 + i * .35f) * .5f + .5f;
                Color tint = Color.Lerp(deep, pale, phase) * ((1 - i / (float)p.oldPos.Length) * .42f);
                tint.A = 0;
                Vector2 ghost = p.oldPos[i] + p.Size * .5f - Main.screenPosition;
                float rotation = p.oldRot[i] - layout.Axis(false).ToRotation();
                Main.EntitySpriteDraw(body, ghost, null, tint, rotation, layout.Grip, scale, SpriteEffects.None);
                Main.EntitySpriteDraw(body, ghost, null, tint * .7f, rotation, layout.Grip, scale * .7f, SpriteEffects.None);
            }
        }
        Main.EntitySpriteDraw(edge, at, null, color * (returning ? 1f : .85f), angle,
            layout.Grip + new Vector2(2), scale, SpriteEffects.None);
        if (returning)
            for (int i = 0; i < 4; i++)
                Main.EntitySpriteDraw(edge, at + (i * MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly).ToRotationVector2() * 2,
                    null, color * .15f, angle, layout.Grip + new Vector2(2), scale, SpriteEffects.None);
        Main.EntitySpriteDraw(body, at, null, returning ? Color.Lerp(color, Color.White, .38f) : Color.Lerp(light, Color.White, .2f),
            angle, layout.Grip, scale, SpriteEffects.None);
    }
}

public sealed class DreadnoughtThrowImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    { Projectile.width = Projectile.height = 2; Projectile.timeLeft = 18; Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true; }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0 || !ChargedOperatorVfx.Visible(Projectile.Center)) return;
        SoundEngine.PlaySound(SoundID.Item68 with { Volume = .9f, MaxInstances = 12 }, Projectile.Center);
        DreadnoughtThrowVisuals.Emit(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX),
            (ChargedOperatorKind)(int)Projectile.ai[0], Projectile.ai[1] == 1 ? 22 : 16, Projectile.ai[1] == 1 ? 13 : 9);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!ChargedOperatorVfx.Visible(Projectile.Center)) return false;
        float progress = 1 - Projectile.timeLeft / 18f, fade = (1 - progress) * (1 - progress);
        var kind = (ChargedOperatorKind)(int)Projectile.ai[0];
        Color tint = ChargedOperatorRules.ColorFor(kind);
        using var pass = new ChargedOperatorVfx.Pass(true);
        ChargedOperatorVfx.Sprite("RuptureRing", Projectile.Center, new Vector2(30 + progress * 140, 18 + progress * 75),
            tint * fade, Projectile.velocity.ToRotation());
        int rays = kind == ChargedOperatorKind.NearlRadiant ? 6 : kind == ChargedOperatorKind.Matoimaru ? 3 : 2;
        for (int i = 0; i < rays; i++)
            ChargedOperatorVfx.Sprite("LinearSpark", Projectile.Center, new Vector2(9, 55 + progress * 110),
                Color.Lerp(tint, Color.White, .65f) * fade, Projectile.velocity.ToRotation() + i * MathHelper.Pi / rays);
        return false;
    }
}

// Petals, curling spirit ribbons, thermite splinters and radiant feather barbs
// share a bounded particle lifetime, but have different motion and silhouettes.
public sealed class DreadnoughtMoteParticle : Particle
{
    private readonly ChargedOperatorKind kind;
    private readonly Color initial;
    private readonly float size;
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public DreadnoughtMoteParticle(Vector2 at, Vector2 velocity, ChargedOperatorKind kind, int life, float size)
    {
        Position = at; Velocity = velocity; this.kind = kind; Lifetime = life; this.size = size;
        initial = Color = ChargedOperatorRules.ColorFor(kind); Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        if (kind == ChargedOperatorKind.Melantha) { Velocity = Velocity.RotatedBy(.11f) * .93f; Rotation += .14f; }
        else if (kind == ChargedOperatorKind.Matoimaru) { Velocity = Velocity.RotatedBy(MathF.Sin(Time * .25f) * .13f) * .94f; Rotation = Velocity.ToRotation(); }
        else if (kind == ChargedOperatorKind.Franka) { Velocity *= .95f; Velocity.Y += .13f; Rotation = Velocity.ToRotation(); }
        else { Velocity *= .92f; Velocity.Y -= .025f; Rotation += .035f; }
        Color = initial * MathF.Pow(1 - LifetimeRatio, 1.7f);
    }
    public override void Draw()
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Vector2 at = Position - Main.screenPosition;
        void Stroke(Vector2 offset, Vector2 extent, float rotation, Color tint) =>
            Main.spriteBatch.Draw(pixel, at + offset, new Rectangle(0, 0, 1, 1), tint, rotation, new Vector2(.5f), extent * size, SpriteEffects.None, 0);
        if (kind == ChargedOperatorKind.Melantha)
        {
            Stroke(Vector2.Zero, new Vector2(8, 3), Rotation, Color);
            Stroke(Vector2.Zero, new Vector2(5, 2), Rotation + .6f, Color * .65f);
        }
        else if (kind == ChargedOperatorKind.Matoimaru)
            for (int i = 0; i < 3; i++) Stroke(new Vector2(-i * 3, MathF.Sin(Time * .3f + i) * 2).RotatedBy(Rotation),
                new Vector2(9 - i * 2, 1.5f), Rotation, Color * (1 - i * .25f));
        else if (kind == ChargedOperatorKind.Franka)
        {
            Stroke(Vector2.Zero, new Vector2(12, 2), Rotation, Color);
            Stroke(Vector2.Zero, new Vector2(4, 1), Rotation + MathHelper.PiOver2, Color.Lerp(Color, Microsoft.Xna.Framework.Color.White * (Color.A / 255f), .5f));
        }
        else
        {
            Stroke(Vector2.Zero, new Vector2(12, 1.5f), Rotation, Color);
            for (int i = -1; i <= 1; i++) Stroke(new Vector2(i * 3, 0).RotatedBy(Rotation), new Vector2(5, 1), Rotation + .7f, Color * .7f);
        }
    }
}

public sealed class DreadnoughtThrowVisualSystem : ModSystem
{
    public override void Unload() => DreadnoughtThrowVisuals.Unload();
}
