using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Local, bounded presentation. Absorption rings never occupy the hostile projectile budget.
public sealed class EvolutionImpactSystem : ModSystem
{
    private struct Pulse
    {
        internal Vector2 Center;
        internal float Radius, Force;
        internal int Age, Duration;
        internal bool Inward;
        internal Color Color, Core;
        internal ulong Tick;
        internal float GapAngle, GapHalfWidth;
        internal int GapCount;
    }
    private static readonly Pulse[] pulses = new Pulse[32];
    private static int cursor;
    private static float shake;
    private static int shakeAge;
    private static ulong lastSoundTick;
    private const string WarpKey = "ArknightsMod:EvolutionPressure";
    private Effect material, warp;
    private bool materialFailed;
    internal static void Kick(Vector2 center, float force)
    {
        if (Main.dedServ || Main.gameMenu) return;
        float proximity = 1 - EvolutionCinematicFrame.Smooth(1000, 2600, Vector2.Distance(Main.LocalPlayer.Center, center));
        shake = Math.Max(shake, Math.Min(10, force * proximity));
        if (proximity > 0 && Main.GameUpdateCount - lastSoundTick >= 10)
        {
            lastSoundTick = Main.GameUpdateCount;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .48f * proximity, Pitch = -.65f, PitchVariance = .06f, MaxInstances = 2 }, center);
        }
    }
    private static bool Pressure(Projectile projectile, out EvolutionHazard hazard)
    {
        hazard = projectile.ModProjectile as EvolutionHazard;
        return hazard?.PressureLive == true && hazard.PressureAge < EvolutionShockwaveRules.Duration;
    }
    internal static void Emit(Vector2 center, float radius, float force, bool inward = false, int duration = 28,
        Color? color = null, Color? core = null)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, center) > 2800 * 2800) return;
        Color theme = color ?? ThemeAt(center);
        Color highlight = core ?? Color.Lerp(theme, new Color(255, 239, 218), .78f);
        // Several rays can fire at the same origin in one update. One coherent front, one sound.
        int slot = -1;
        for (int i = 0; i < pulses.Length; i++)
            if (pulses[i].Duration > 0 && pulses[i].Tick == Main.GameUpdateCount && pulses[i].Inward == inward &&
                Vector2.DistanceSquared(pulses[i].Center, center) < 48 * 48) { slot = i; break; }
        if (slot < 0) { slot = cursor; cursor = (cursor + 1) % pulses.Length; }
        else { radius = Math.Max(radius, pulses[slot].Radius); force = Math.Max(force, pulses[slot].Force); duration = Math.Max(duration, pulses[slot].Duration); }
        pulses[slot] = new Pulse { Center = center, Radius = Math.Max(24, radius), Force = force, Duration = Math.Max(1, duration),
            Inward = inward, Color = theme, Core = highlight, Tick = Main.GameUpdateCount };
        float proximity = 1 - EvolutionCinematicFrame.Smooth(1000, 2600, Vector2.Distance(Main.LocalPlayer.Center, center));
        shake = Math.Max(shake, Math.Min(10, force * proximity));
        if (!inward && radius >= 600 && force >= 3.5f && Main.GameUpdateCount - lastSoundTick >= 10)
        {
            lastSoundTick = Main.GameUpdateCount;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .48f * proximity, Pitch = -.65f, PitchVariance = .06f, MaxInstances = 2 }, center);
        }
    }
    private static Color ThemeAt(Vector2 center)
    {
        float distance = 2400 * 2400;
        Color theme = EvolutionVisuals.Blood;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Evolution boss && Vector2.DistanceSquared(npc.Center, center) < distance)
            { distance = Vector2.DistanceSquared(npc.Center, center); theme = boss.ThemeColor; }
        return theme;
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        for (int i = 0; i < pulses.Length; i++) if (pulses[i].Duration > 0 && ++pulses[i].Age >= pulses[i].Duration) pulses[i].Duration = 0;
        shake *= .84f; shakeAge++;
        UpdateWarp();
    }
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu || EvolutionCinematics.Reduced || !EvolutionCinematics.ShakeEnabled) return;
        Main.screenPosition += new Vector2(MathF.Sin(shakeAge * 2.43f), MathF.Cos(shakeAge * 1.83f)) * shake;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        foreach (var p in pulses) if (p.Duration > 0) { any = true; break; }
        if (!any) foreach (Projectile projectile in Main.ActiveProjectiles)
            if (Pressure(projectile, out _)) { any = true; break; }
        if (!any) return;
        EnsureMaterial();
        GraphicsDevice device = Main.instance.GraphicsDevice;
        if (material != null && !EvolutionCinematics.Reduced)
        {
            Texture oldNoise = device.Textures[1];
            SamplerState oldSampler = device.SamplerStates[1];
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                device.Textures[1] = EvolutionVisuals.Asset("PressureNoise");
                device.SamplerStates[1] = SamplerState.LinearWrap;
                int fronts = 0;
                for (int offset = 0; offset < pulses.Length; offset++)
                {
                    var p = pulses[(cursor - 1 - offset + pulses.Length * 2) % pulses.Length];
                    if (Visible(p) && !p.Inward)
                    {
                        EvolutionShockwaveVisuals.DrawFront(Main.spriteBatch, material, EvolutionVisuals.Asset("PressureNoise"),
                            p.Center - Main.screenPosition, p.Radius, p.Age, p.Duration, Power(p), p.Color, p.Core);
                        if (++fronts == 8) break;
                    }
                }
                foreach (Projectile projectile in Main.ActiveProjectiles)
                    if (Pressure(projectile, out var hazard)) hazard.DrawPressureMaterial(material);
            }
            finally { Main.spriteBatch.End(); device.Textures[1] = oldNoise; device.SamplerStates[1] = oldSampler; }
        }
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        DrawPulses(material == null);
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (Pressure(projectile, out var hazard)) hazard.DrawPressureDetails(material == null);
        Main.spriteBatch.End();
    }
    private static float Power(Pulse p) => Math.Clamp(.65f + p.Force * .07f, .65f, 1.3f);
    private static bool Visible(Pulse p)
    {
        if (p.Duration <= 0) return false;
        Vector2 at = Vector2.Transform(p.Center - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
        float extent = (p.Radius * 1.22f + 100) * Main.GameViewMatrix.Zoom.X;
        return at.X > -extent && at.Y > -extent && at.X < Main.screenWidth + extent && at.Y < Main.screenHeight + extent;
    }
    internal static void DrawPulses(bool fallback = true)
    {
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        foreach (var p in pulses)
        {
            if (!Visible(p)) continue;
            if (!p.Inward)
            {
                EvolutionShockwaveVisuals.DrawDebris(Main.spriteBatch, EvolutionVisuals.Asset("Bloom"), TextureAssets.MagicPixel.Value, ring,
                    p.Center - Main.screenPosition, p.Radius, p.Age, p.Duration, Power(p), p.Color, p.Core, EvolutionCinematics.Reduced, fallback);
                continue;
            }
            float u = p.Age / (float)p.Duration;
            float radius = p.Radius * (p.Inward ? MathF.Pow(1 - u, 1.7f) : MathF.Sqrt(u));
            float opacity = MathF.Sin(u * MathHelper.Pi) * (EvolutionCinematics.Reduced ? .15f : .4f);
            for (int layer = 0; layer < 3; layer++)
                Main.spriteBatch.Draw(ring, p.Center - Main.screenPosition, null,
                    ((layer == 1 ? p.Core : p.Color) with { A = 0 }) * (opacity / (layer + 1)),
                    u * (layer % 2 == 0 ? 1 : -1), ring.Size() * .5f, new Vector2(radius * 2, radius * 1.8f) * (1 + layer * .09f) / ring.Size(), SpriteEffects.None, 0);
            if (p.Inward && !EvolutionCinematics.Reduced) for (int i = 0; i < 24; i++)
            {
                Vector2 direction = (i * MathHelper.TwoPi / 24 + u * .35f).ToRotationVector2();
                EvolutionVisuals.Line(p.Center + direction * radius, p.Center + direction * (radius + 28 * (1 - u)), p.Color * opacity, 2);
            }
        }
    }
    private void EnsureMaterial()
    {
        if (material != null || materialFailed) return;
        try
        {
            byte[] bytes = Mod.GetFileBytes("Assets/Effects/EvolutionShockwave.fxc");
            material = new Effect(Main.instance.GraphicsDevice, bytes);
            material.CurrentTechnique = material.Techniques["PressureFront"];
            warp = new Effect(Main.instance.GraphicsDevice, bytes);
            warp.CurrentTechnique = warp.Techniques["PressureWarp"];
            Filters.Scene[WarpKey] = new Filter(new ScreenShaderData(new Ref<Effect>(warp), "WarpPass").UseOpacity(0), EffectPriority.High);
            Filters.Scene[WarpKey].Load();
        }
        catch (Exception error)
        {
            materialFailed = true;
            material?.Dispose(); warp?.Dispose(); material = warp = null;
            Mod.Logger.Warn("Evolution pressure-front material unavailable; using sprite fallback: " + error.Message);
        }
    }
    private void UpdateWarp()
    {
        if (warp == null) return;
        Filter filter = Filters.Scene[WarpKey];
        var config = ModContent.GetInstance<EvolutionCinematicConfig>();
        if (Main.gameMenu || Main.LocalPlayer.dead || config.ReducedEffects || !config.ScreenFilters) { ResetWarp(); return; }
        Pulse strongest = default;
        float score = 0;
        foreach (var p in pulses)
        {
            if (p.Duration <= 0 || p.Inward || p.Radius < 400 || p.Force < 3) continue;
            float proximity = 1 - EvolutionCinematicFrame.Smooth(1200, 2600, Vector2.Distance(Main.LocalPlayer.Center, p.Center));
            float weight = EvolutionShockwaveVisuals.Opacity(EvolutionShockwaveVisuals.Progress(p.Age, p.Duration)) * p.Force * proximity;
            if (weight > score) { strongest = p; score = weight; }
        }
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (!Pressure(projectile, out var hazard) || hazard.PressureAge < 0) continue;
            float proximity = 1 - EvolutionCinematicFrame.Smooth(1200, 2600, Vector2.Distance(Main.LocalPlayer.Center, projectile.Center));
            float weight = EvolutionShockwaveVisuals.Opacity(EvolutionShockwaveVisuals.Progress(hazard.PressureAge, EvolutionShockwaveRules.Duration)) * 5.5f * proximity;
            if (weight <= score) continue;
            score = weight;
            strongest = new Pulse { Center = projectile.Center, Radius = EvolutionShockwaveRules.Reach, Age = hazard.PressureAge,
                Duration = EvolutionShockwaveRules.Duration, GapAngle = hazard.Parameter, GapCount = EvolutionShockwaveRules.GapCount,
                GapHalfWidth = EvolutionShockwaveRules.GapHalfWidth };
        }
        if (score <= .01f) { ResetWarp(); return; }
        float u = EvolutionShockwaveVisuals.Progress(strongest.Age, strongest.Duration);
        warp.Parameters["uWaveRadius"].SetValue(EvolutionShockwaveVisuals.Radius(strongest.Radius, u));
        warp.Parameters["uWaveWidth"].SetValue(EvolutionShockwaveVisuals.Width(strongest.Radius, u));
        warp.Parameters["uWaveStrength"].SetValue(Math.Min(22, score * 4));
        EvolutionShockwaveVisuals.SetGaps(warp, strongest.GapAngle, strongest.GapCount, strongest.GapHalfWidth);
        if (!filter.IsActive()) Filters.Scene.Activate(WarpKey, strongest.Center);
        filter.GetShader().UseTargetPosition(strongest.Center).UseOpacity(1);
    }
    private void ResetWarp()
    {
        if (warp == null) return;
        warp.Parameters["uWaveStrength"].SetValue(0);
        Filter filter = Filters.Scene[WarpKey];
        filter?.GetShader().UseOpacity(0);
        if (filter?.IsActive() == true) filter.Deactivate();
    }
    public override void OnWorldUnload() { Array.Clear(pulses); shake = 0; cursor = 0; shakeAge = 0; lastSoundTick = 0; ResetWarp(); }
    public override void Unload()
    {
        OnWorldUnload();
        Effect oldMaterial = material, oldWarp = warp;
        material = warp = null; materialFailed = false;
        Main.QueueMainThreadAction(() => { oldMaterial?.Dispose(); oldWarp?.Dispose(); });
    }
}
