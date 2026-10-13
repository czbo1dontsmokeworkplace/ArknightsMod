using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

// Original grayscale masks: straight RGBA, rendered with SourceAlpha additive blending.
// No Calamity resource loads or runtime references.
public sealed class MousseVisuals : ModSystem
{
    internal const float RangeMultiplier = 1.25f;
    internal static readonly Color Pale = new(255, 244, 172);
    private static Texture2D blade, bloom, crescent, crescentCore;

    public override void Load() {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() => {
            blade = Mask(128, 256, (x, y) => {
                float height = (y + 1f) * .5f;
                float width = .035f + .32f * MathF.Pow(height, .65f);
                float tip = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(height * 20f, 0, 1));
                float baseFade = MathHelper.Clamp((1f - height) * 20f, 0, 1);
                return MathF.Exp(-x * x / (width * width) * 3f) * tip * baseFade;
            });
            bloom = Mask(96, 96, (x, y) => MathF.Pow(Math.Max(0, 1f - x * x - y * y), 4f));
            float SlashAlpha(float x, float y) {
                float radius = MathF.Sqrt(x * x + y * y);
                float angle = MathF.Atan2(y, x);
                float arc = MathF.Pow(Math.Max(0, MathF.Cos(angle * .53f)), .8f);
                float center = .66f + .012f * MathF.Sin(angle * 9f);
                // A thin leading tip becomes a broad lower/right sweep, rather than a closed ring.
                float thickness = .025f + .24f * MathHelper.Clamp((y + .9f) / 1.8f, 0, 1)
                    + .12f * Math.Max(0f, MathF.Cos(angle));
                float ridge = MathF.Exp(-MathF.Pow((radius - center) / thickness, 2f) * 2f);
                float boundary = MathHelper.Clamp((1f - radius) * 14f, 0, 1);
                return ridge * arc * boundary * MathHelper.Clamp((x + .94f) * 6f, 0, 1);
            }
            crescent = Mask(256, 256, SlashAlpha);
            crescentCore = Mask(256, 256, (x, y) => MathF.Pow(SlashAlpha(x, y), 3f));
        });
    }

    private static Texture2D Mask(int width, int height, Func<float, float, float> alpha) {
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = new Color(255, 255, 255,
                    (int)(255f * MathHelper.Clamp(alpha(2f * x / (width - 1f) - 1f, 2f * y / (height - 1f) - 1f), 0, 1)));
        Texture2D texture = new(Main.graphics.GraphicsDevice, width, height);
        texture.SetData(pixels);
        return texture;
    }

    public override void Unload() {
        Texture2D oldBlade = blade, oldBloom = bloom, oldCrescent = crescent, oldCore = crescentCore;
        blade = bloom = crescent = crescentCore = null;
        if (!Main.dedServ) Main.QueueMainThreadAction(() => {
            oldBlade?.Dispose(); oldBloom?.Dispose(); oldCrescent?.Dispose(); oldCore?.Dispose();
        });
    }

    private static void BeginGlow() {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }

    private static void EndGlow() {
        Main.spriteBatch.End();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
    }

    // size is always world pixels, never a raw SpriteBatch scale.
    private static void DrawPixels(Texture2D texture, Vector2 position, Vector2 size, Color color,
        float rotation, Vector2 origin, SpriteEffects flip = SpriteEffects.None) {
        if (texture == null || size.X <= 0f || size.Y <= 0f || !float.IsFinite(size.X + size.Y)) return;
        size = Vector2.Min(size, new Vector2(1200f));
        Main.spriteBatch.Draw(texture, position - Main.screenPosition, null, color, rotation,
            origin * texture.Size(), size / texture.Size(), flip, 0f);
    }

    internal static void DrawGlove(Projectile p, Player owner, float power, Color lightColor) {
        Vector2 hand = p.Center + new Vector2(0f, owner.gfxOffY);
        Texture2D glove = TextureAssets.Projectile[p.type].Value;
        Main.EntitySpriteDraw(glove, hand - Main.screenPosition, null, lightColor, p.rotation + MathHelper.PiOver2,
            new Vector2(glove.Width * .5f, glove.Height - 2f), 1f, SpriteEffects.None);
        if (blade == null || power <= .01f) return;
        power = MathHelper.Clamp(power, 0f, 1.5f);
        Vector2 tip = hand + p.rotation.ToRotationVector2() * (glove.Height - 2f);
        // Auger's base blade dimensions: 1960 * .014 * power by 2368 * .014 * power².
        Vector2 size = new Vector2(27.44f * power, 33.152f * power * power) * RangeMultiplier * p.scale;
        BeginGlow();
        try {
            DrawPixels(blade, tip, size, Pale * .85f, p.rotation + MathHelper.PiOver2, new Vector2(.5f, 1f));
            DrawPixels(blade, tip, size * new Vector2(.45f, .98f), Color.White * .9f,
                p.rotation + MathHelper.PiOver2, new Vector2(.5f, 1f));
            DrawPixels(bloom, tip, new Vector2(32f, 18f) * power * p.scale,
                Pale * .55f, p.rotation, new Vector2(.5f));
        }
        finally { EndGlow(); }
    }

    internal static void DrawSlash(Projectile p) {
        if (crescent == null) return;
        float age = 25f - p.timeLeft;
        float fade = MathF.Pow(MathHelper.Clamp(p.timeLeft / 25f, 0, 1), 1.5f);
        float squeeze = MathHelper.Lerp(1f, .18f, MousseGlovePunch.Ease(age / 25f, 2, 2));
        float unit = MousseLightSlash.Reach * p.ai[1];
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 position = p.Center + aim * unit * .5f;
        Vector2 size = new(unit * 2f, unit * 1.2f * squeeze);
        SpriteEffects flip = (p.ai[0] < 0) == (aim.X >= 0) ? SpriteEffects.FlipVertically : SpriteEffects.None;
        BeginGlow();
        try {
            DrawPixels(crescent, position, size, Pale * (fade * .8f), aim.ToRotation(), new Vector2(.5f), flip);
            DrawPixels(crescentCore, position, size, Color.White * (fade * .65f),
                aim.ToRotation(), new Vector2(.5f), flip);
        }
        finally { EndGlow(); }
    }

    internal static void EmitSlash(Vector2 center, Vector2 aim, float scale, float swing) {
        if (Main.dedServ || Vector2.DistanceSquared(center, Main.LocalPlayer.Center) > 2400f * 2400f) return;
        for (int i = 0; i < 30 && ParticleManager.activeParticles.Count < 1500; i++) {
            float t = i / 29f;
            float angle = MathHelper.Lerp(-.7f, .7f, t);
            float radius = (70f + 80f * MathF.Pow(1f - Math.Abs(angle), 1.5f)) * RangeMultiplier * scale;
            Vector2 direction = aim.RotatedBy(angle);
            Vector2 place = center + direction * radius - aim * 45f * RangeMultiplier * scale;
            Vector2 velocity = direction.RotatedBy(-angle * .6f) * Main.rand.NextFloat(2f, 5f) * scale;
            float weight = swing < 0 ? 1f - t : t;
            new MousseLightShard(place, velocity, Main.rand.Next(12, 23), (3f + weight * 6f) * scale,
                Color.Lerp(Pale, Color.White, weight)).Spawn();
            if (i % 6 == 0)
                CasterSparkles.Spawn(place, velocity * .35f, Pale, .4f, 12, aim.ToRotation());
        }
    }

    internal static void DrawShard(Vector2 position, Vector2 velocity, float length, Color color) {
        if (blade == null) return;
        DrawPixels(blade, position, new Vector2(Math.Max(2f, length * .3f), length), color,
            velocity.ToRotation() + MathHelper.PiOver2, new Vector2(.5f));
    }
}

public sealed class MousseLightShard : Particle
{
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly float length;
    private readonly Color tint;

    public MousseLightShard(Vector2 position, Vector2 velocity, int lifetime, float length, Color tint) {
        Position = position; Velocity = velocity; Lifetime = lifetime;
        this.length = MathHelper.Clamp(length, 2f, 28f); this.tint = tint;
    }
    public override void Update() { Velocity *= .90f; }
    public override void Draw() {
        float fade = MathF.Pow(1f - LifetimeRatio, 1.5f);
        MousseVisuals.DrawShard(Position, Velocity, length * (.6f + fade), tint * fade);
    }
}
