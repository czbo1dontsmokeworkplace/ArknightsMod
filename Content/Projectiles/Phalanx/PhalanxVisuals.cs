using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Phalanx;

public sealed class PhalanxVisuals : ModSystem
{
    private static Effect barrier;
    private static bool failed;
    public static Color Palette(int tier) => tier switch
    {
        0 => new Color(255, 200, 92), 1 => new Color(245, 92, 57), _ => new Color(235, 132, 239)
    };
    public override void Unload()
    {
        Effect old = barrier; barrier = null; failed = false;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
    // Dimensions are final screen/world pixels, never raw SpriteBatch scale factors.
    // Each call normalizes against the actual texture dimensions. Reject corrupt geometry.
    public static bool TrySpriteScale(Vector2 textureSize, Vector2 pixels, out Vector2 scale)
    {
        scale = Vector2.Zero;
        if (!float.IsFinite(textureSize.X) || !float.IsFinite(textureSize.Y)
            || !float.IsFinite(pixels.X) || !float.IsFinite(pixels.Y)
            || textureSize.X <= 0 || textureSize.Y <= 0
            || pixels.X <= 0 || pixels.Y <= 0 || pixels.X > 800 || pixels.Y > 800) return false;
        scale = pixels / textureSize;
        return true;
    }
    public static void Sprite(Texture2D texture, Vector2 center, Vector2 pixels, Color color,
        float rotation = 0, SpriteEffects effects = SpriteEffects.None)
    {
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y) || !float.IsFinite(rotation)
            || !TrySpriteScale(texture.Size(), pixels, out Vector2 scale)) return;
        Main.spriteBatch.Draw(texture, center, texture.Bounds, color, rotation, texture.Size() * .5f,
            scale, effects, 0);
    }
    public static void Glow(Vector2 center, Vector2 pixels, Color color, float rotation = 0)
        => Sprite(TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value, center, pixels, color, rotation);
    public static void Shockwave(Vector2 center, Vector2 pixels, Color color, float rotation = 0)
        => Sprite(ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/PassengerImpactRing").Value,
            center, pixels, color, rotation);
    public static void Burst(Vector2 center, int tier, int count, float speed)
    {
        if (Main.dedServ) return;
        Color color = Palette(tier);
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(.3f, 1.3f);
            Dust dust = Dust.NewDustPerfect(center, DustID.TintableDustLighted, velocity, 80, color, Main.rand.NextFloat(1f, 1.8f));
            dust.noGravity = true;
        }
    }
    public static void Shield(Vector2 center, int tier, float opacity, float flash, int layers)
    {
        if (Main.dedServ || opacity <= .001f) return;
        float rotation = Main.GlobalTimeWrappedHourly * .22f;
        // Only the requested spherical shield. No seals, polygons, orbit lines or wireframe fallback.
        if (!DrawSurface(center, tier, opacity, flash, rotation))
            Glow(center - Main.screenPosition, new Vector2(232), Palette(tier) * (opacity * .2f));
    }
    private static bool DrawSurface(Vector2 center, int tier, float opacity, float flash, float rotation)
    {
        if (failed) return false;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Texture oldTexture = device.Textures[1];
        SamplerState oldSampler = device.SamplerStates[1];
        bool ended = false, begun = false;
        try
        {
            // Adapted from the user's FranBarrierRenderer / FranBarrierSurfaceShader.
            // Keep the compiled effect local: no dependency on Fran or Luminance.
            barrier ??= new Effect(device, ModContent.GetInstance<global::ArknightsMod.ArknightsMod>()
                .GetFileBytes("Assets/Effects/Phalanx/PhalanxBarrier.fxc"));
            Color color = Palette(tier);
            barrier.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
            barrier.Parameters["uShieldColor"].SetValue((color * .55f).ToVector3());
            barrier.Parameters["uEdgeColor"].SetValue(Color.Lerp(color, Color.White, flash * .75f).ToVector3());
            barrier.Parameters["uCoreColor"].SetValue(Color.Lerp(color, Color.White, .65f).ToVector3());
            barrier.Parameters["uCharge"].SetValue(opacity);
            barrier.Parameters["uHitFlash"].SetValue(flash);
            barrier.Parameters["uOpacity"].SetValue(.86f * opacity);
            barrier.Parameters["uRotation"].SetValue(rotation);
            Texture2D noise = ModContent.Request<Texture2D>("ArknightsMod/Assets/Effects/Phalanx/NoiseSoft").Value;
            Main.spriteBatch.End(); ended = true;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, barrier, Main.GameViewMatrix.TransformationMatrix);
            begun = true;
            device.Textures[1] = noise; device.SamplerStates[1] = SamplerState.LinearWrap;
            float diameter = (232 + flash * 20) * (1 + MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * .018f);
            for (int layer = 0; layer < 2; layer++)
            {
                barrier.Parameters["uLayer"].SetValue((float)layer);
                barrier.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(noise, center - Main.screenPosition, null, Color.White, 0,
                    noise.Size() / 2, new Vector2(diameter * (1 + layer * .025f)) / noise.Size(), SpriteEffects.None, 0);
            }
            return true;
        }
        catch (Exception e)
        {
            failed = true;
            ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().Logger.Warn("Phalanx shield shader failed; using soft glow fallback. " + e);
            return false;
        }
        finally
        {
            if (begun) Main.spriteBatch.End();
            if (ended) Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            device.Textures[1] = oldTexture; device.SamplerStates[1] = oldSampler;
        }
    }
}
