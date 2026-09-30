using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// A dedicated world pass under the boss sprites. Never restart a batch inside projectile PreDraw.
public sealed class EvolutionAxisRenderer : ModSystem
{
    private Effect material;
    private bool failed;
    public override void Unload()
    {
        Effect old = material; material = null; failed = false;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EvolutionHazard h && Visible(h)) { any = true; break; }
        if (!any) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        if (material == null && !failed)
        {
            try { material = new Effect(device, Mod.GetFileBytes("Assets/Effects/EvolutionHemalAxis.fxc")); }
            catch (Exception e) { failed = true; Mod.Logger.Warn("Evolution axis material unavailable; using soft energy fallback. " + e); }
        }
        Texture2D noise = EvolutionVisuals.Asset("EnergyNoise");
        Texture oldNoise = device.Textures[1];
        SamplerState oldSampler = device.SamplerStates[1];
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, material, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is not EvolutionHazard h || !Visible(h)) continue;
                if (material != null)
                    DrawAxisMaterial(Main.spriteBatch, device, material, noise, p.Center - Main.screenPosition,
                        h.AxisDirection.ToRotation(), h.Parameter, h.Age - h.FireAge, h.AxisOpacity * (EvolutionCinematics.Reduced ? .75f : 1));
                else
                    for (int ray = 0; ray < EvolutionRules.AxisRayCount; ray++)
                    {
                        Vector2 direction = h.AxisRayDirection(ray), center = p.Center + direction * (h.Parameter * .5f);
                        EvolutionVisuals.Glow(center, EvolutionVisuals.Blood * h.AxisOpacity, new Vector2(h.Parameter, 360), direction.ToRotation());
                        EvolutionVisuals.Glow(center, EvolutionVisuals.Core * (.7f * h.AxisOpacity), new Vector2(h.Parameter, 132), direction.ToRotation());
                    }
            }
        }
        finally { Main.spriteBatch.End(); device.Textures[1] = oldNoise; device.SamplerStates[1] = oldSampler; }
    }
    private static bool Visible(EvolutionHazard h) => h.IsAxis && h.AxisOpacity > 0 && h.Parent is Evolution boss && boss.Serial == h.Serial;
    internal static void DrawAxisMaterial(SpriteBatch batch, GraphicsDevice device, Effect effect, Texture2D noise,
        Vector2 origin, float rotation, float reach, float age, float opacity)
    {
        for (int ray = 0; ray < EvolutionRules.AxisRayCount; ray++)
        {
            float angle = rotation + ray * MathHelper.TwoPi / EvolutionRules.AxisRayCount;
            DrawMaterial(batch, device, effect, noise, origin + angle.ToRotationVector2() * (reach * .5f), angle, reach, age, opacity);
        }
    }
    internal static void DrawMaterial(SpriteBatch batch, GraphicsDevice device, Effect effect, Texture2D noise,
        Vector2 screenCenter, float rotation, float length, float age, float opacity)
    {
        effect.Parameters["uTime"].SetValue(age / 60f);
        effect.Parameters["uOpacity"].SetValue(opacity);
        device.Textures[1] = noise; device.SamplerStates[1] = SamplerState.LinearWrap;
        effect.CurrentTechnique.Passes[0].Apply();
        batch.Draw(noise, screenCenter, null, Color.White, rotation, noise.Size() * .5f,
            new Vector2(length, EvolutionRules.AxisVisualWidth) / noise.Size(), SpriteEffects.None, 0);
    }
}
