using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Content.SwingHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

internal static class TyphonAstralVisuals
{
    private static readonly Dictionary<string, Texture2D> textures = new();
    private static Effect shader;
    private static BlendState darkTrailBlend;
    private static readonly Dictionary<Texture2D, Texture2D> alphaMasks = new();
    private static Texture2D arrowSilhouette;
    // Calamity's additive-only art may have fully opaque black outside its light shape.
    // Materialize coverage before using it in an alpha blend; never alter shared source assets.
    internal static Texture2D AlphaMask(Texture2D source)
    {
        if (alphaMasks.TryGetValue(source, out Texture2D mask)) return mask;
        Color[] pixels = new Color[source.Width * source.Height]; source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i]; byte coverage = Math.Min(p.A, Math.Max(p.R, Math.Max(p.G, p.B)));
            // FadeStreak also reaches the canvas edge. Feather that edge for dark alpha rendering.
            int x = i % source.Width, y = i / source.Width;
            float edge = Math.Min(Math.Min(x, source.Width - 1 - x), Math.Min(y, source.Height - 1 - y));
            coverage = (byte)(coverage * MathHelper.SmoothStep(0, 1, Math.Clamp(edge / 3f, 0, 1)));
            pixels[i] = new Color(coverage, coverage, coverage, coverage);
        }
        mask = new Texture2D(Main.spriteBatch.GraphicsDevice, source.Width, source.Height);
        mask.SetData(pixels); alphaMasks[source] = mask; return mask;
    }
    private static Texture2D Texture(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D texture))
            textures[name] = texture = ModContent.Request<Texture2D>(name == "OriginalArrow"
                ? "ArknightsMod/Content/Projectiles/Sniper/Typhon/TyphonArrow"
                : "ArknightsMod/Content/Textures/TyphonAstral/" + name, AssetRequestMode.ImmediateLoad).Value;
        return texture;
    }
    internal static void Unload()
    {
        textures.Clear(); shader = null;
        Texture2D[] owned = new Texture2D[alphaMasks.Count]; alphaMasks.Values.CopyTo(owned, 0); alphaMasks.Clear();
        Texture2D outline = arrowSilhouette; arrowSilhouette = null;
        BlendState blend = darkTrailBlend; darkTrailBlend = null;
        Main.QueueMainThreadAction(() => { foreach (Texture2D t in owned) t.Dispose(); outline?.Dispose(); blend?.Dispose(); });
    }
    internal static Color Tint(float phase) => Color.Lerp(new Color(246, 189, 104), new Color(190, 124, 244), .5f + MathF.Sin(phase) * .5f);
    internal static Color DarkTint(float amount) => Color.Lerp(new Color(78, 24, 112), new Color(7, 3, 13), Math.Clamp(amount, 0, 1));
    internal static void DrawHead(Vector2 center, float rotation, float scale, float phase)
    {
        DrawArrow(center, rotation, scale, phase, true);
    }
    internal static void DrawArrow(Vector2 center, float rotation, float scale, float phase, bool outlined)
    {
        Texture2D arrow = Texture("OriginalArrow");
        Vector2 position = center - Main.screenPosition, origin = arrow.Size() / 2;
        // The source arrow points upward; +Pi/2 aligns its tip with velocity.
        float angle = rotation + MathHelper.PiOver2;
        if (outlined)
        {
            if (arrowSilhouette == null)
            {
                Color[] pixels = new Color[arrow.Width * arrow.Height]; arrow.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++)
                { byte a = pixels[i].A; pixels[i] = new Color(a, a, a, a); }
                arrowSilhouette = new Texture2D(Main.spriteBatch.GraphicsDevice, arrow.Width, arrow.Height);
                arrowSilhouette.SetData(pixels);
            }
            for (int i = 0; i < 8; i++)
                Main.EntitySpriteDraw(arrowSilhouette, position + (MathHelper.TwoPi * i / 8).ToRotationVector2() * 1.4f,
                    null, Color.White, angle, origin, scale, SpriteEffects.None);
        }
        Color tint = outlined ? Color.Lerp(Tint(Main.GlobalTimeWrappedHourly * 2 + phase), Color.White, .3f) : new Color(177, 141, 204);
        Main.EntitySpriteDraw(arrow, position, null, tint, angle, origin, scale, SpriteEffects.None);
    }
    internal static void DrawShot(Projectile projectile)
    {
        DrawTrail(projectile);
        DrawArrow(projectile.Center, projectile.rotation, projectile.scale, projectile.identity * .17f, projectile.ai[0] == 1);
    }
    internal static void DrawTrail(Projectile p)
    {
        List<Vector2> points = new() { p.Center };
        float length = 0;
        foreach (Vector2 old in p.oldPos)
        {
            if (old == Vector2.Zero) break;
            Vector2 next = old + p.Size / 2;
            float step = Vector2.Distance(points[^1], next);
            if (step > 100 || length + step > 460 || !float.IsFinite(step)) break;
            if (step < .2f) continue;
            length += step; points.Add(next);
        }
        if (points.Count < 3) return;
        shader ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/TyphonAstralTrail", AssetRequestMode.ImmediateLoad).Value;
        Texture2D flow = Texture("EternityStreak"), noise = TextureAssets.Extra[189].Value;
        bool dark = p.ai[0] != 1;
        shader.Parameters["uColor"].SetValue((dark ? new Color(20, 2, 32) : Tint(Main.GlobalTimeWrappedHourly * 2)).ToVector3());
        shader.Parameters["uSecondaryColor"].SetValue((dark ? Color.Black : Tint(Main.GlobalTimeWrappedHourly * 2 + 1.3f)).ToVector3());
        shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
        shader.Parameters["uWorldViewProjection"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1));
        Vertex[] vertices = new Vertex[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            float t = i / (float)(points.Count - 1);
            Vector2 forward = (points[Math.Max(0, i - 1)] - points[Math.Min(points.Count - 1, i + 1)]).SafeNormalize(p.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 side = new Vector2(-forward.Y, forward.X) * (p.scale * 22 * (1 - t * .75f));
            // The original shader needs a THREE-component UV with a nonzero correction factor.
            vertices[i * 2] = new Vertex(points[i] + side - Main.screenPosition, new Vector3(t, 0, 1), Color.White);
            vertices[i * 2 + 1] = new Vertex(points[i] - side - Main.screenPosition, new Vector3(t, 1, 1), Color.White);
        }
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        darkTrailBlend ??= new BlendState { ColorSourceBlend = Blend.BlendFactor,
            ColorDestinationBlend = Blend.InverseSourceAlpha, AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha, BlendFactor = new Color(47, 9, 72) };
        Main.spriteBatch.End();
        Color oldBlendFactor = device.BlendFactor;
        Texture old1 = device.Textures[1], old2 = device.Textures[2];
        SamplerState oldSampler1 = device.SamplerStates[1], oldSampler2 = device.SamplerStates[2];
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, dark ? darkTrailBlend : BlendState.Additive, SamplerState.LinearWrap,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            device.Textures[1] = flow; device.Textures[2] = noise;
            device.SamplerStates[1] = device.SamplerStates[2] = SamplerState.LinearWrap;
            shader.CurrentTechnique.Passes["PiercePass"].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
        }
        finally
        {
            device.Textures[1] = old1; device.Textures[2] = old2;
            device.SamplerStates[1] = oldSampler1; device.SamplerStates[2] = oldSampler2;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            device.BlendFactor = oldBlendFactor;
        }
    }
    internal static void Fire(Vector2 center, Vector2 aim, bool large)
    {
        if (!ChargedOperatorVfx.Visible(center) || ParticleManager.activeParticles.Count > 1000) return;
        for (int i = 0; i < (large ? 24 : 10); i++)
        {
            float angle = i * MathHelper.PiOver2;
            Vector2 velocity = angle.ToRotationVector2().RotatedBy(aim.ToRotation()) * Main.rand.NextFloat(2, large ? 9 : 5);
            new TyphonStarParticle(center, velocity, large ? DarkTint(i % 4 / 3f) : Tint(i), large ? 1 : .6f, 16 + i % 8, large).Spawn();
        }
        new TyphonMuzzleBloom(center, aim, large ? 1 : .6f, large).Spawn();
        new T_Sniper_Breach_Particle(center, aim, global::ArknightsMod.Content.Items.Weapons.Charged.ChargedOperatorKind.Fartooth, large, false, false, 0)
            { Color = large ? DarkTint(.35f) : new Color(236, 221, 149) }.Spawn();
    }
    internal static void Flight(Projectile p)
    {
        if (!ChargedOperatorVfx.Visible(p.Center) || ParticleManager.activeParticles.Count > 1000 || p.timeLeft % 2 != 0) return;
        bool dark = p.ai[0] != 1;
        new TyphonStarParticle(p.Center - p.velocity * 2, -p.velocity * .05f,
            dark ? DarkTint(.55f) : Tint(p.identity + Main.GlobalTimeWrappedHourly), p.scale * .32f, 15, dark).Spawn();
        if (dark && p.timeLeft % 4 == 0)
            new T_Sniper_Impact_Cloud(p.Center, -p.velocity * .02f, DarkTint(.85f), 24, 18, p.identity).Spawn();
    }
}

public sealed class TyphonMuzzleBloom : Particle
{
    private readonly bool dark;
    public override string TexturePath => "ArknightsMod/Content/Textures/TyphonAstral/BloomCircle";
    public override BlendState DrawBlendState => dark ? BlendState.AlphaBlend : BlendState.Additive;
    public TyphonMuzzleBloom(Vector2 center, Vector2 aim, float size, bool dark = false)
    { Position = center; Velocity = aim * .6f; Rotation = aim.ToRotation(); Scale = size; Lifetime = 18; this.dark = dark; }
    public override void Draw()
    {
        Texture2D art = TyphonAstralVisuals.AlphaMask(Texture);
        float fade = MathF.Pow(1 - LifetimeRatio, 2), growth = .5f + MathF.Min(1, Time / 3f) * .5f;
        for (int i = 0; i < 2; i++)
            Main.spriteBatch.Draw(art, Position - Main.screenPosition, null,
                (dark ? TyphonAstralVisuals.DarkTint(i * .5f) : TyphonAstralVisuals.Tint(i * 2)) * fade * .65f,
                Rotation + i * MathHelper.PiOver2, Texture.Size() / 2,
                new Vector2(94, 48) * (Scale * growth) / Texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(art, Position - Main.screenPosition, null, (dark ? TyphonAstralVisuals.DarkTint(.7f) : Color.White) * fade * .8f,
            0, Texture.Size() / 2, new Vector2(25) * (Scale * growth) / Texture.Size(), SpriteEffects.None, 0);
    }
}

public sealed class TyphonStarParticle : Particle
{
    private readonly bool dark;
    public override string TexturePath => "ArknightsMod/Content/Textures/TyphonAstral/FadeStreak";
    public override BlendState DrawBlendState => dark ? BlendState.AlphaBlend : BlendState.Additive;
    public TyphonStarParticle(Vector2 center, Vector2 velocity, Color tint, float size, int life, bool dark = false)
    { Position = center; Velocity = velocity; Color = tint; Scale = size; Lifetime = life; Rotation = velocity.ToRotation(); this.dark = dark; }
    public override void Update() { Velocity *= .9f; Rotation += .035f; }
    public override void Draw()
    {
        Texture2D art = TyphonAstralVisuals.AlphaMask(Texture);
        float fade = MathF.Pow(1 - Math.Clamp(LifetimeRatio, 0, 1), 1.5f);
        for (int i = 0; i < 4; i++)
            Main.spriteBatch.Draw(art, Position - Main.screenPosition, null, Color * fade * .6f,
                Rotation + i * MathHelper.PiOver2, new Vector2(Texture.Width / 2f, 0),
                new Vector2(20, 28) * (Scale * fade) / Texture.Size(), SpriteEffects.None, 0);
    }
}

public sealed class TyphonAstralSystem : ModSystem
{
    public override void Unload() => TyphonAstralVisuals.Unload();
}
