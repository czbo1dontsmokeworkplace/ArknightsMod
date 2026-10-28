using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaVisuals : ModSystem
{
    public static readonly Color Cyan = new(0, 158, 255), Gold = new(255, 176, 46);
    public static readonly Color Abyss = new(3, 17, 72), Azure = new(0, 72, 230), Crest = new(35, 240, 255);
    private static Effect material;
    private static Texture2D glow, ring;
    private static bool failed;
    private static readonly Dictionary<string, Texture2D> artCache = new();
    internal static float? ClockOverride;
    public static float Clock => ClockOverride ?? Main.GlobalTimeWrappedHourly;
    public static Texture2D Art(string name)
    {
        if (!artCache.TryGetValue(name, out var texture)) artCache[name] = texture = ModContent.Request<Texture2D>(Isharmla.Root + name).Value;
        return texture;
    }
    public static Texture2D Glow
    {
        get
        {
            if (glow != null) return glow;
            glow = new Texture2D(Main.instance.GraphicsDevice, 64, 64);
            Color[] pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float radius = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f)) / 31.5f;
                float alpha = MathF.Pow(Math.Max(0, 1 - radius), 2);
                pixels[y * 64 + x] = Color.White * alpha;
            }
            glow.SetData(pixels); return glow;
        }
    }
    public static Texture2D RingTexture
    {
        get
        {
            if (ring != null) return ring;
            ring = new Texture2D(Main.instance.GraphicsDevice, 160, 160); Color[] pixels = new Color[25600];
            for (int y = 0; y < 160; y++) for (int x = 0; x < 160; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(79.5f));
                float a = Math.Max(0, 1 - Math.Abs(r - 72) / 5);
                pixels[y * 160 + x] = Color.White * a;
            }
            ring.SetData(pixels); return ring;
        }
    }
    public override void Unload()
    {
        var fx = material; var g = glow; var r = ring; material = null; glow = ring = null; failed = false; artCache.Clear(); ClockOverride = null;
        TidalParticles.Clear();
        if (!Main.dedServ) Main.QueueMainThreadAction(() => { fx?.Dispose(); g?.Dispose(); r?.Dispose(); TidalParticles.DisposeTextures(); });
    }
    public override void OnWorldUnload() => TidalParticles.Clear();
    private static void EnsureEffect()
    {
        if (material != null || failed || Main.dedServ) return;
        try { material = new Effect(Main.instance.GraphicsDevice, ModContent.GetInstance<IsharmlaVisuals>().Mod.GetFileBytes("Assets/Effects/IsharmlaTide.fxc")); }
        catch (Exception e) { failed = true; ModContent.GetInstance<IsharmlaVisuals>().Mod.Logger.Warn("Isharmla water material unavailable: " + e.Message); }
    }
    public static void Orb(Vector2 center, Vector2 radius, Color color) => Main.spriteBatch.Draw(Glow, center - Main.screenPosition,
        null, color, 0, new Vector2(32), radius / 25, SpriteEffects.None, 0);
    public static void Line(Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(),
            new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    public static void Ring(Vector2 center, float radius, Color color, float width = 2)
    {
        Vector2 previous = center + new Vector2(radius, 0);
        for (int i = 1; i <= 64; i++)
        {
            Vector2 next = center + (i * MathHelper.TwoPi / 64).ToRotationVector2() * radius;
            Line(previous, next, color, width); previous = next;
        }
    }
    public static void DrawHuman(SpriteBatch batch, Isharmla boss, Vector2 screen)
        => IsharmlaPresentation.DrawHuman(batch, boss, screen);
    public static void DrawDragonPart(SpriteBatch batch, string art, Vector2 center, Vector2 origin, float rotation,
        float scale, TideStage stage, float opacity, Vector2 screen)
    {
        Texture2D texture = Art(art);
        DrawSpriteRim(texture, center - screen, origin, rotation, scale,
            (stage == TideStage.LastSong ? Gold : Cyan) * (opacity * (stage >= TideStage.Covenant ? .65f : .36f)));
        Color tint = stage >= TideStage.Covenant ? Color.Lerp(Color.White, new Color(145, 238, 255), .25f) : Color.White;
        batch.Draw(texture, center - screen, null, tint * opacity, rotation, origin, scale, SpriteEffects.None, 0);
        if (stage >= TideStage.Covenant)
        {
            EnsureEffect(); if (material == null) return;
            material.Parameters["uTime"].SetValue(Clock);
            material.Parameters["uImage"].SetValue(texture);
            material.Parameters["uStrength"].SetValue(stage == TideStage.LastSong ? 1f : stage == TideStage.LastTide ? .65f : .28f);
            material.CurrentTechnique = material.Techniques["Hull"];
            batch.End();
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
            batch.Draw(texture, center - screen, null, (stage == TideStage.LastSong ? Gold : Cyan) * opacity, rotation, origin, scale, SpriteEffects.None, 0);
            Restore(batch);
        }
    }
    public static void DrawWaterMaterial(Vector2 from, Vector2 to, float width, Color color, string technique = "Water")
    {
        EnsureEffect(); if (material == null) return;
        material.Parameters["uTime"].SetValue(Clock);
        material.Parameters["uSize"].SetValue(new Vector2(Vector2.Distance(from, to), width));
        material.Parameters["uDetail"].SetValue(Art("ParticleAtlasV2"));
        material.Parameters["uStrength"].SetValue(1f); material.CurrentTechnique = material.Techniques[technique];
        var batch = Main.spriteBatch; Vector2 delta = to - from;
        batch.End();
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        material.Parameters["uImage"].SetValue(pixel);
        batch.Draw(pixel, from - Main.screenPosition, null, color, delta.ToRotation(),
            new Vector2(0, pixel.Height * .5f), new Vector2(delta.Length() / pixel.Width, width / pixel.Height), SpriteEffects.None, 0);
        Restore(batch);
    }
    private static void Restore(SpriteBatch batch)
    {
        batch.End(); batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }
    public static void Mote(Vector2 center, Vector2 velocity, float size)
        => TidalParticles.Emit(TideParticle.Lumen, center, velocity, new Vector2(size * 7), Cyan, 48);
    public static void Burst(Vector2 center, int count)
    {
        if (Main.dedServ) return;
        TidalParticles.Burst(center, count);
        if (!ClockOverride.HasValue) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/Isharmla/Impact") { Volume = .35f, MaxInstances = 2 }, center);
    }
    public static void Ambient(Vector2 center, TideStage stage, int timer)
    {
        if (timer % 4 != 0) return;
        Vector2 edge = Main.rand.NextVector2CircularEdge(72, 72);
        TidalParticles.Emit(timer % 12 == 0 ? TideParticle.Prism : TideParticle.Bubble,
            center + edge, edge.RotatedBy(MathHelper.PiOver2) * .016f - new Vector2(0, .35f), new Vector2(5), Cyan, 75);
        if (timer % 16 == 0) TidalParticles.Emit(TideParticle.Note, center + new Vector2(30, -20), new Vector2(.35f, -1), new Vector2(9), Gold, 85);
    }
    public static void Transition(Vector2 center, int timer)
    {
        if (timer % 3 == 0)
        {
            Vector2 edge = Main.rand.NextVector2CircularEdge(145, 115);
            TidalParticles.Emit(TideParticle.Prism, center + edge, -edge * .025f + edge.RotatedBy(MathHelper.PiOver2) * .014f,
                new Vector2(7), timer % 2 == 0 ? Gold : Cyan, 38);
        }
        if (timer % 22 == 0) TidalParticles.Emit(TideParticle.Ripple, center, Vector2.Zero, new Vector2(40, 18), Gold, 60);
    }
    public static void Shake(float amount)
    {
        if (!Main.dedServ && Main.LocalPlayer.active) Main.LocalPlayer.GetModPlayer<IsharmlaPlayer>().Shake = Math.Min(6, amount);
    }
    public override void PostUpdateEverything() { if (!Main.dedServ) TidalParticles.Update(); }
    public override void PostDrawTiles() { if (!Main.dedServ) TidalParticles.Draw(); }
}
