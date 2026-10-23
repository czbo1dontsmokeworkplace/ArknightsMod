using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class MaterialistTechRenderer : ModSystem
{
    private static Effect shader;
    private static bool failed;
    internal static bool HullActive => shader != null && !failed && !MaterialistVisuals.Reduced;
    public override void Unload()
    {
        var effect = shader; shader = null; failed = false;
        if (effect != null) Main.QueueMainThreadAction(effect.Dispose);
    }
    internal static Effect CreateEffect(GraphicsDevice device, byte[] bytes) => new(device, bytes);
    internal static void Draw(MaterialistAntagonizer boss)
    {
        if (Main.dedServ) return;
        EnsureShader();
        if (shader == null || MaterialistVisuals.Reduced) return;
        DrawCommands(boss);
    }
    private static void EnsureShader()
    {
        if (shader == null && !failed)
        {
            try { shader = CreateEffect(Main.instance.GraphicsDevice, ModContent.GetInstance<MaterialistTechRenderer>().Mod.GetFileBytes("Assets/Effects/MaterialistTech.fxc")); }
            catch (Exception error) { failed = true; ModContent.GetInstance<MaterialistTechRenderer>().Mod.Logger.Warn("Command display shader unavailable; keeping geometric telegraphs. " + error); }
        }
    }
    private static void DrawCommands(MaterialistAntagonizer boss)
    {
        {
            Configure(shader, Main.GlobalTimeWrappedHourly, new Vector2(380, 190), 0, 1);
            if (boss.Order is FlightOrder.Deploy or FlightOrder.Arrival or FlightOrder.Transition || boss.Shield > 0)
                DrawPanel(shader, boss.NPC.Center, new Vector2(380, 190), MaterialistVisuals.Cyan *
                    (boss.Order == FlightOrder.Arrival ? .6f * MaterialistFormation.Smooth(boss.Timer / 24f) * (1 - MaterialistFormation.Smooth((boss.Timer - 178) / 32f)) : .6f));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.TryGetGlobalNPC<MaterialistWing>(out var wing) || !wing.BelongsTo(boss) || wing.Retreating) continue;
                if (MaterialistWing.Role(npc) == WingRole.Raptor) continue;
                Color dutyColor = MaterialistWing.DutyColor(MaterialistRules.Duty(boss.Phase, wing.Slot));
                DrawPanel(shader, npc.Center, new Vector2(npc.width + 38, npc.height + 30), dutyColor * (wing.Age < 72 ? .7f : .20f));
                // A second, scanning hull layer is visible during launch without replacing the chassis.
                if (wing.Age < 55)
                {
                    var sprite = MaterialistWing.Sprite(npc, wing.Age);
                    Configure(shader, Main.GlobalTimeWrappedHourly, sprite.Frame.Size(), 1, MathHelper.Clamp(wing.Age / 38f, 0, 1));
                    shader.Parameters["uImage"].SetValue(sprite.Texture);
                    shader.Parameters["uUvBounds"].SetValue(new Vector4(sprite.Frame.X / (float)sprite.Texture.Width,
                        sprite.Frame.Y / (float)sprite.Texture.Height, sprite.Frame.Width / (float)sprite.Texture.Width, sprite.Frame.Height / (float)sprite.Texture.Height));
                    shader.CurrentTechnique.Passes[0].Apply();
                    BeginMaterial(shader);
                    try
                    {
                    Main.spriteBatch.Draw(sprite.Texture, npc.Center - Main.screenPosition, sprite.Frame, MaterialistVisuals.Cyan * .8f,
                        npc.rotation, sprite.Frame.Size() * .5f, npc.scale * MathHelper.Lerp(.3f, 1, MaterialistFormation.Smooth(wing.Age / 38f)),
                        npc.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                    }
                    finally { Main.spriteBatch.End(); }
                }
            }
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is MaterialistHazard h && h.BelongsTo(boss) && h.Kind == DroneShot.Bombardment && h.Age < h.Delay)
                    DrawPanel(shader, p.Center, new Vector2(MaterialistRules.BombRadius * 2), MaterialistVisuals.Amber * (.36f * h.Visibility));
                if (p.ModProjectile is MaterialistHazard scan && scan.BelongsTo(boss) && scan.Kind == DroneShot.ScanBurst)
                    DrawPanel(shader, p.Center, scan.ScanSize, MaterialistVisuals.Cyan * ((scan.Age < scan.Delay ? .72f : 1f) * scan.Visibility));
            }
            DrawEnergyTrails(boss);
        }
    }
    // The native cyan laser is recoloured by luminance, preserving its animated edge detail.
    // This colour conversion remains enabled in reduced-effects mode.
    internal static void NativeLaser(Action draw)
    {
        EnsureShader();
        if (shader == null) { draw(); return; }
        Main.spriteBatch.End();
        try
        {
            shader.CurrentTechnique = shader.Techniques["MaterialistLaser"];
            BeginMaterial(shader);
            try { draw(); }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void NativeLaserTexture(Texture2D texture)
    {
        if (shader != null) shader.Parameters["uImage"].SetValue(texture);
    }
    internal static bool LockShieldMaterial(Vector2 center, Vector2 size, float rotation, float progress, Color tint)
    {
        EnsureShader();
        if (!HullActive) return false;
        Main.spriteBatch.End();
        try
        {
            shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uSize"].SetValue(size);
            shader.Parameters["uReveal"].SetValue(progress);
            shader.CurrentTechnique = shader.Techniques["MaterialistLockShield"];
            Texture2D texture = TextureAssets.MagicPixel.Value;
            shader.Parameters["uImage"].SetValue(texture);
            BeginMaterial(shader);
            try
            {
                Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, tint, rotation,
                    texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
            }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        return true;
    }
    internal static void Configure(Effect effect, float time, Vector2 size, float mode, float reveal)
    {
        effect.Parameters["uTime"].SetValue(time);
        effect.Parameters["uSize"].SetValue(size);
        effect.CurrentTechnique = effect.Techniques[mode > 5.5f ? "MaterialistSeverance" : mode > 4.5f ? "MaterialistDisassembly" :
            mode > 3.5f ? "MaterialistOutline" : mode > 2.5f ? "MaterialistEnergy" : mode > 1.5f ? "MaterialistOverclock" : mode > .5f ? "MaterialistHull" : "MaterialistTech"];
        effect.Parameters["uUvBounds"].SetValue(new Vector4(0, 0, 1, 1));
        effect.Parameters["uReveal"].SetValue(reveal);
    }
    internal static void OverclockChassis(Texture2D texture, Vector2 position, Rectangle? frame, float angle, Vector2 scale, SpriteEffects flip, float opacity)
        => ChassisMaterial(texture, position, frame, angle, scale, flip, Color.White * opacity, 2);
    internal static bool ChassisMaterial(Texture2D texture, Vector2 position, Rectangle? frame, float angle, Vector2 scale,
        SpriteEffects flip, Color tint, float mode, float reveal = 1, float? materialTime = null)
    {
        EnsureShader();
        if (shader == null || failed || MaterialistVisuals.Reduced && mode != 4 || tint.A == 0) return false;
        Rectangle source = frame ?? texture.Bounds;
        Main.spriteBatch.End();
        try
        {
            Configure(shader, materialTime ?? Main.GlobalTimeWrappedHourly, source.Size(), mode, reveal);
            shader.Parameters["uImage"].SetValue(texture);
            shader.Parameters["uUvBounds"].SetValue(new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height,
                source.Width / (float)texture.Width, source.Height / (float)texture.Height));
            BeginMaterial(shader);
            try { Main.spriteBatch.Draw(texture, position - Main.screenPosition, source, tint, angle, source.Size() * .5f, scale, flip, 0); }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        return true;
    }
    internal static bool DeathCut(Vector2 center, Vector2 size, float rotation, Color tint, float progress, float time)
    {
        EnsureShader();
        if (!HullActive || tint.A == 0) return false;
        Main.spriteBatch.End();
        try
        {
            Configure(shader, time, size, 6, progress);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            shader.Parameters["uImage"].SetValue(pixel);
            BeginMaterial(shader);
            try
            {
                Main.spriteBatch.Draw(pixel, center - Main.screenPosition, null, tint, rotation,
                    new Vector2(.5f), size, SpriteEffects.None, 0);
            }
            finally { Main.spriteBatch.End(); }
        }
        finally
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        return true;
    }
    private static void DrawEnergyTrails(MaterialistAntagonizer boss)
    {
        Configure(shader, Main.GlobalTimeWrappedHourly, new Vector2(100, 24), 3, 1);
        BeginMaterial(shader);
        try
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is not MaterialistHazard h || !h.BelongsTo(boss) || h.Age < h.Delay ||
                    h.Kind is not (DroneShot.Prism or DroneShot.Seeker or DroneShot.CoreLance or DroneShot.ArcSpark or DroneShot.OrbitSpark)) continue;
                Color tint = MaterialistVisuals.AmmoColor(h) * h.Visibility;
                for (int i = 1; i < p.oldPos.Length; i++)
                {
                    if (p.oldPos[i] == Vector2.Zero || p.oldPos[i - 1] == Vector2.Zero) continue;
                    Vector2 a = p.oldPos[i] + p.Size * .5f, b = p.oldPos[i - 1] + p.Size * .5f;
                    float fade = 1 - i / (float)p.oldPos.Length;
                    Main.spriteBatch.Draw(pixel, (a + b) * .5f - Main.screenPosition, null, tint * (fade * .75f),
                        (b - a).ToRotation(), pixel.Size() * .5f, new Vector2(Math.Max(1, Vector2.Distance(a, b)), 22 * fade) / pixel.Size(), SpriteEffects.None, 0);
                }
            }
        }
        finally { Main.spriteBatch.End(); }
    }
    private static void DrawPanel(Effect effect, Vector2 point, Vector2 size, Color color)
    {
        Configure(effect, Main.GlobalTimeWrappedHourly, size, 0, 1);
        Texture2D texture = TextureAssets.MagicPixel.Value;
        effect.Parameters["uImage"].SetValue(texture);
        effect.CurrentTechnique.Passes[0].Apply();
        BeginMaterial(effect);
        try { Main.spriteBatch.Draw(texture, point - Main.screenPosition, null, color, 0, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0); }
        finally { Main.spriteBatch.End(); }
    }
    private static void BeginMaterial(Effect effect) => Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
        DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
}
