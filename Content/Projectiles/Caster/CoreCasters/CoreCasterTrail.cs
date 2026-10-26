using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// One continuous tapered surface following sampled collision positions; no joined sprite rectangles.
internal static class CoreCasterTrail
{
    private static Effect material;
    private static BlendState radiance;
    internal static BlendState Radiance => radiance ??= new BlendState {
        ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One
    };
    private static readonly Vector2[] points = new Vector2[66];
    private static readonly float[] distances = new float[66];
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[132];
    internal static float LengthFor(CoreCasterKind kind, int mode) => kind switch {
        CoreCasterKind.Durin => 110, CoreCasterKind.Steward => 165, CoreCasterKind.Nightmare => 245,
        CoreCasterKind.Absinthe => 195, CoreCasterKind.Eyjafjalla => mode == 3 ? 130 : 175, _ => 210
    };
    internal static int Build(Projectile p, CoreCasterKind kind, int mode)
    {
        float limit = LengthFor(kind, mode), distance = 0;
        float width = kind switch { CoreCasterKind.Durin => 17, CoreCasterKind.Steward => 22,
            CoreCasterKind.Haze => 31, CoreCasterKind.Nightmare => 28, CoreCasterKind.Absinthe => 15,
            CoreCasterKind.Eyjafjalla => mode == 3 ? 29 : 36, _ => 27 };
        if (mode > 0) width *= 1.08f;
        int count = 1; points[0] = p.Center; distances[0] = 0;
        foreach (Vector2 old in p.oldPos) {
            if (old == Vector2.Zero || count >= points.Length) break;
            Vector2 at = old + p.Size / 2;
            float step = Vector2.Distance(at, points[count - 1]);
            if (!float.IsFinite(step) || step > 96) break;
            if (step < .25f) continue;
            if (distance + step > limit) { at = Vector2.Lerp(points[count - 1], at, (limit - distance) / step); step = limit - distance; }
            points[count] = at; distances[count++] = distance += step;
            if (distance >= limit) break;
        }
        for (int i = 0; i < count; i++) {
            Vector2 tangent = points[Math.Max(0, i - 1)] - points[Math.Min(count - 1, i + 1)];
            if (tangent.LengthSquared() < .01f) tangent = p.velocity;
            tangent = CoreCasterFlight.Unit(tangent);
            Vector2 normal = new(-tangent.Y, tangent.X);
            float age = distances[i] / limit;
            float radius = width * .5f * MathF.Pow(1 - age, .62f);
            Color alpha = i == count - 1 ? Color.Transparent : Color.White;
            Vector2 position = points[i] - Main.screenPosition;
            mesh[i * 2] = new(new Vector3(position + normal * radius, 0), alpha, new(age, 0));
            mesh[i * 2 + 1] = new(new Vector3(position - normal * radius, 0), alpha, new(age, 1));
        }
        return count * 2;
    }
    internal static void Draw(Projectile p, CoreCasterKind kind, int mode)
    {
        int count = Build(p, kind, mode); if (count < 4) return;
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/CoreCasterEnergyTrail", AssetRequestMode.ImmediateLoad).Value;
        Color color = kind == CoreCasterKind.Haze && mode == 2 ? CoreCasterArt.Crimson : CoreCasterArt.ColorFor(kind);
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["Outer"].SetValue(color.ToVector3());
        material.Parameters["Inner"].SetValue(CoreCasterArt.Accent(kind, color).ToVector3());
        material.Parameters["Shadow"].SetValue(CoreCasterPalette.For(CoreCasterPalette.Key(kind, kind == CoreCasterKind.Haze && mode == 2)).Shadow.ToVector3());
        material.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly + p.identity * .137f);
        material.Parameters["Style"].SetValue((float)kind);
        var device = Main.spriteBatch.GraphicsDevice; Main.spriteBatch.End();
        try {
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes[0].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, mesh, 0, count - 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void Unload() { material = null; radiance?.Dispose(); radiance = null; }
}
public sealed class CoreCasterVisualSystem : ModSystem
{
    public override void Unload()
    {
        if (!Main.dedServ) Main.QueueMainThreadAction(() => { CoreCasterTrail.Unload(); CoreCasterArt.Unload(); });
    }
}
