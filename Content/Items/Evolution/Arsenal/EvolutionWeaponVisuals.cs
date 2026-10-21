using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

internal static class EvolutionWeaponVisuals
{
    internal static readonly Color Blood = new(228, 26, 60), Ivory = new(255, 207, 173);
    internal static Effect Material;
    internal static void Sparks(Vector2 point, int count, float speed, float spatialScale = 1)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++)
            OperatorTextureEffects.Mote(point, Main.rand.NextVector2Unit() * Main.rand.NextFloat(.5f, speed), i % 3 == 0 ? Ivory : Blood, Main.rand.NextFloat(3, 7) * spatialScale, 20);
    }
    internal static void Glow(Vector2 point, float diameter, float opacity)
        => EvolutionVisuals.Glow(point, Blood * opacity, new Vector2(diameter));
    internal static void Ring(Vector2 point, float diameter, float opacity, float rotation)
    {
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        Main.EntitySpriteDraw(ring, point - Main.screenPosition, null, (Blood with { A = 0 }) * opacity,
            rotation, ring.Size() * .5f, new Vector2(diameter, diameter * .77f) / ring.Size(), SpriteEffects.None);
    }
    internal static void FallbackLine(Vector2 start, Vector2 end, float width, float opacity)
    {
        EvolutionVisuals.Glow((start + end) * .5f, Blood * opacity, new Vector2(Vector2.Distance(start, end), width * 2), (end - start).ToRotation());
        EvolutionVisuals.Line(start, end, Ivory * opacity, Math.Max(1, width * .12f), false);
    }
    private static void Quad(Vector2 center, Vector2 size, float rotation, int kind, float opacity, float seed, float offset = 0, float span = 1)
    {
        if (Material == null || opacity <= .001f || size.X <= .01f || size.Y <= .01f) return;
        Material.Parameters["uTime"].SetValue((float)Main.GlobalTimeWrappedHourly * (kind == 2 ? 2.2f : 1));
        Material.Parameters["uOpacity"].SetValue(Math.Clamp(opacity, 0, 1));
        Material.Parameters["uLength"].SetValue(size.X / Math.Max(span, .001f));
        Material.Parameters["uKind"].SetValue((float)kind); Material.Parameters["uSeed"].SetValue(seed);
        Material.Parameters["uOffset"].SetValue(offset); Material.Parameters["uSpan"].SetValue(span);
        Material.CurrentTechnique.Passes[0].Apply();
        Texture2D texture = TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, Color.White, rotation, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
    }
    internal static void Ribbon(Vector2 start, Vector2 end, float width, float opacity, float seed, float offset = 0, float span = 1)
    {
        Vector2 delta = end - start;
        Quad((start + end) * .5f, new Vector2(delta.Length() + .6f, width), delta.ToRotation(), 1, opacity, seed, offset, span);
    }
    internal static void Core(Vector2 point, float diameter, float opacity, float seed)
        => Quad(point, new Vector2(diameter), seed * .1f, 0, opacity, seed);
    internal static void SeverRibbon(Vector2 start, Vector2 end, float width, float opacity, float seed, float offset, float span)
        => Quad((start + end) * .5f, new Vector2(Vector2.Distance(start, end) + .7f, width), (end - start).ToRotation(), 3, opacity, seed, offset, span);
    internal static void SeverHead(Vector2 point, float rotation, float seed)
        => Quad(point, new Vector2(50, 32), rotation, 4, .88f, seed);
    internal static void CutRibbon(Vector2 start, Vector2 end, float width, float opacity, float seed, float offset, float span)
        => Quad((start + end) * .5f, new Vector2(Vector2.Distance(start, end) + .9f, width), (end - start).ToRotation(), 5, opacity, seed, offset, span);
    internal static void Mist(Vector2 point, float diameter, float opacity, float seed, float rotation = 0)
        => Quad(point, new Vector2(diameter), seed + rotation, 2, opacity, seed);
    internal static void Powder(Vector2 point, float diameter, float opacity, float seed, float rotation)
        => Quad(point, new Vector2(diameter), rotation, 6, opacity, seed);
    private static float MatrixOpacity(TerminusMatrix matrix)
        => Math.Clamp(matrix.Age / 10, 0, 1) * Math.Clamp((EvolutionArsenalRules.MatrixLife - matrix.Age) / 24f, 0, 1);
    internal static void MatrixDetails(TerminusMatrix matrix)
    {
        Vector2 center = matrix.Projectile.Center;
        float age = matrix.Age, opacity = MatrixOpacity(matrix);
        Ring(center, 198 + MathF.Sin(age * .12f) * 8, opacity * .4f, age * .025f);
        Ring(center, 142, opacity * .3f, -age * .04f);
        Glow(center, 100, opacity * .38f);
        for (int i = 0; i < EvolutionArsenalRules.CoreCount; i++)
        {
            if (age >= EvolutionArsenalRules.CoreFireTick(i)) continue;
            Vector2 point = center + EvolutionArsenalRules.CoreOffset(i, age);
            float fired = age - EvolutionArsenalRules.CoreFireTick(i);
            float flash = fired >= 0 ? Math.Clamp(1 - fired / 9, 0, 1) : 0;
            Glow(point, 34 + flash * 26, opacity * (.5f + flash * .5f));
            Ring(point, 28 + flash * 25, opacity * (.35f + flash * .4f), -age * .08f + i);
            for (int j = 1; j <= 4; j++)
                Glow(center + EvolutionArsenalRules.CoreOffset(i, age - j * 1.7f), 11 - j, opacity * (.19f - j * .03f));
            if (!EvolutionWeaponRenderer.Ready)
            {
                FallbackLine(center, point, 5, opacity * .35f);
                EvolutionVisuals.Glow(point, Ivory * (opacity * .7f), new Vector2(11));
            }
        }
    }
    internal static void MatrixMaterial(TerminusMatrix matrix)
    {
        Vector2 center = matrix.Projectile.Center;
        float age = matrix.Age, opacity = MatrixOpacity(matrix);
        for (int i = 0; i < EvolutionArsenalRules.CoreCount; i++)
        {
            if (age >= EvolutionArsenalRules.CoreFireTick(i)) continue;
            Vector2 end = center + EvolutionArsenalRules.CoreOffset(i, age);
            Vector2 normal = (end - center).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Vector2 previous = center;
            for (int j = 1; j <= 7; j++)
            {
                float p = j / 7f;
                Vector2 point = Vector2.Lerp(center, end, p) + normal * MathF.Sin(p * MathHelper.Pi) * (12 + MathF.Sin(age * .11f + i) * 10);
                Ribbon(previous, point, 9, opacity * .45f, i, (j - 1) / 7f, 1 / 7f);
                previous = point;
            }
            float flash = Math.Clamp(1 - MathF.Abs(age - EvolutionArsenalRules.CoreFireTick(i)) / 8, 0, 1);
            Core(end, 35 + flash * 15, opacity, i * 1.71f);
        }
        float contraction = age < 54 ? 1 + MathF.Sin(age * .19f) * .08f : MathHelper.Lerp(1, .25f, Math.Clamp((age - 54) / 24, 0, 1));
        Core(center, 76 * contraction, opacity, 11);
    }
    internal static void SwordDetails(OriginSwing swing)
    {
        Vector2 start = swing.Projectile.Center, end = swing.Tip(swing.Progress), delta = end - start;
        float opacity = OriginCutVisuals.Envelope(swing.Progress);
        Main.instance?.LoadItem(ItemID.BreakerBlade);
        Texture2D blade = TextureAssets.Item[ItemID.BreakerBlade].Value;
        float scale = delta.Length() / blade.Size().Length();
        Main.EntitySpriteDraw(blade, (start + end) * .5f - Main.screenPosition, null, Color.Lerp(Color.White, Blood, .28f) * Math.Max(.3f, opacity),
            delta.ToRotation() + MathHelper.PiOver4, blade.Size() * .5f, scale, SpriteEffects.None);
        Glow(end, 42 * EvolutionArsenalRules.OriginScale, opacity * .7f);
        Vector2 edge = delta.SafeNormalize(Vector2.UnitX);
        EvolutionVisuals.Line(start + delta * .3f, end + edge * (8 * EvolutionArsenalRules.OriginScale), new Color(255, 188, 199) * (opacity * .75f), 1.8f * EvolutionArsenalRules.OriginScale, false);
        if (!EvolutionWeaponRenderer.Ready) FallbackLine(start, end, 22 * EvolutionArsenalRules.OriginScale, opacity);
    }
    internal static void SwordMaterial(OriginSwing swing)
    {
        float p = swing.Progress, opacity = OriginCutVisuals.Envelope(p);
        Vector2 center = swing.Projectile.Center, tip = swing.Tip(p);
        CutRibbon(center, tip, (swing.Stage == 2 ? 42 : 27) * EvolutionArsenalRules.OriginScale, opacity, swing.Stage * 3, 0, 1);
        if (swing.Stage == 2)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 normal = (tip - center).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * side;
                CutRibbon(center + (normal * 34 - swing.Aim * 24) * EvolutionArsenalRules.OriginScale, tip + normal * (7 * EvolutionArsenalRules.OriginScale), 17 * EvolutionArsenalRules.OriginScale, opacity * .55f, side + 5, 0, 1);
            }
            return;
        }
        const int segments = 28;
        // Two ticks of history: a sharp leading edge and a compact, rapidly dissolving crescent.
        float window = 2.3f / Math.Max(6, swing.Projectile.ai[2]);
        Vector2 previous = swing.Tip(Math.Max(0, p - window));
        for (int i = 1; i <= segments; i++)
        {
            float f = i / (float)segments;
            Vector2 point = swing.Tip(Math.Max(0, p - window * (1 - f)));
            float strength = opacity * f * f;
            CutRibbon(previous, point, (30 + 82 * f) * EvolutionArsenalRules.OriginScale, strength * .95f, swing.Stage,
                (i - 1) / (float)segments, 1f / segments);
            Vector2 innerA = Vector2.Lerp(center, previous, .8f), innerB = Vector2.Lerp(center, point, .8f);
            CutRibbon(innerA, innerB, (11 + 17 * f) * EvolutionArsenalRules.OriginScale, strength * .55f, swing.Stage + 9,
                (i - 1) / (float)segments, 1f / segments);
            Vector2 outerA = Vector2.Lerp(center, previous, 1.055f), outerB = Vector2.Lerp(center, point, 1.055f);
            CutRibbon(outerA, outerB, (8 + 9 * f) * EvolutionArsenalRules.OriginScale, strength * .65f, swing.Stage + 16,
                (i - 1) / (float)segments, 1f / segments);
            previous = point;
        }
    }
    internal static void ShardDetails(Projectile projectile)
    {
        Glow(projectile.Center, 26, .6f);
        Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);
        EvolutionVisuals.Line(projectile.Center - direction * 9, projectile.Center + direction * 9, Ivory * .85f, 2, false);
        if (!EvolutionWeaponRenderer.Ready)
            for (int i = 1; i < projectile.oldPos.Length; i++)
            {
                if (projectile.oldPos[i] == Vector2.Zero) break;
                FallbackLine(projectile.oldPos[i - 1] + projectile.Size * .5f, projectile.oldPos[i] + projectile.Size * .5f, 8 * (1 - i / 12f), .5f * (1 - i / 12f));
            }
    }
}

public sealed class EvolutionWeaponRenderer : ModSystem
{
    private Effect material;
    private bool failed;
    internal static bool Ready;
    public override void Unload()
    {
        Effect old = material; material = null; failed = Ready = false; EvolutionWeaponVisuals.Material = null;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EvolutionArsenalProjectile && (p.ModProjectile is TerminusRay || p.Distance(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) < 2200)) { any = true; break; }
        if (!any) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        if (material == null && !failed)
        {
            try { material = new Effect(device, Mod.GetFileBytes("Assets/Effects/EvolutionWeaponMaterial.fxc")); }
            catch (Exception error) { failed = true; Mod.Logger.Warn("Evolution weapon material unavailable: " + error); }
        }
        Ready = material != null;
        if (!Ready) return;
        Texture oldNoise = device.Textures[1]; SamplerState oldSampler = device.SamplerStates[1];
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            device.Textures[1] = EvolutionVisuals.Asset("EnergyNoise"); device.SamplerStates[1] = SamplerState.LinearWrap;
            EvolutionWeaponVisuals.Material = material;
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is EvolutionArsenalProjectile arsenal && (arsenal is TerminusRay || p.Distance(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) < 2200))
                    arsenal.DrawMaterial();
        }
        finally
        {
            EvolutionWeaponVisuals.Material = null; Main.spriteBatch.End(); device.Textures[1] = oldNoise; device.SamplerStates[1] = oldSampler;
        }
    }
}
