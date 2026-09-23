using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Local, bounded presentation. Absorption rings never occupy the hostile projectile budget.
public sealed class EvolutionImpactSystem : ModSystem
{
    private struct Pulse { internal Vector2 Center; internal float Radius; internal int Age, Duration; internal bool Inward; }
    private static readonly Pulse[] pulses = new Pulse[32];
    private static int cursor;
    private static float shake;
    private static int shakeAge;
    internal static void Emit(Vector2 center, float radius, float force, bool inward = false, int duration = 28)
    {
        if (Main.dedServ || Main.gameMenu || Vector2.DistanceSquared(Main.LocalPlayer.Center, center) > 2800 * 2800) return;
        pulses[cursor++ % pulses.Length] = new Pulse { Center = center, Radius = radius, Duration = duration, Inward = inward };
        float proximity = 1 - EvolutionCinematicFrame.Smooth(1000, 2600, Vector2.Distance(Main.LocalPlayer.Center, center));
        shake = Math.Max(shake, Math.Min(10, force * proximity));
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        for (int i = 0; i < pulses.Length; i++) if (pulses[i].Duration > 0 && ++pulses[i].Age >= pulses[i].Duration) pulses[i].Duration = 0;
        shake *= .84f; shakeAge++;
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
        if (!any) return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        DrawPulses();
        Main.spriteBatch.End();
    }
    internal static void DrawPulses()
    {
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        foreach (var p in pulses)
        {
            if (p.Duration <= 0) continue;
            float u = p.Age / (float)p.Duration;
            float radius = p.Radius * (p.Inward ? MathF.Pow(1 - u, 1.7f) : MathF.Sqrt(u));
            float opacity = MathF.Sin(u * MathHelper.Pi) * (EvolutionCinematics.Reduced ? .15f : .4f);
            for (int layer = 0; layer < 3; layer++)
                Main.spriteBatch.Draw(ring, p.Center - Main.screenPosition, null,
                    ((layer == 1 ? EvolutionVisuals.Core : EvolutionVisuals.Blood) with { A = 0 }) * (opacity / (layer + 1)),
                    u * (layer % 2 == 0 ? 1 : -1), ring.Size() * .5f, new Vector2(radius * 2, radius * 1.8f) * (1 + layer * .09f) / ring.Size(), SpriteEffects.None, 0);
            if (p.Inward && !EvolutionCinematics.Reduced) for (int i = 0; i < 24; i++)
            {
                Vector2 direction = (i * MathHelper.TwoPi / 24 + u * .35f).ToRotationVector2();
                EvolutionVisuals.Line(p.Center + direction * radius, p.Center + direction * (radius + 28 * (1 - u)), EvolutionVisuals.Blood * opacity, 2);
            }
        }
    }
    public override void OnWorldUnload() { Array.Clear(pulses); shake = 0; cursor = 0; }
    public override void Unload() => OnWorldUnload();
}
