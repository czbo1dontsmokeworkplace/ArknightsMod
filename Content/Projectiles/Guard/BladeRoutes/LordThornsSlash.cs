using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

// Same three strips, widths, handed masks and subtract/alpha passes as jiciwd2.
// Samples stay relative to the player, so moving cannot tear the arc apart.
public sealed class LordThornsSlash : ModSystem
{
    private static BlendState subtract;
    public override void Unload()
        => Main.QueueMainThreadAction(() => { subtract?.Dispose(); subtract = null; });

    internal static void Draw(Vector2[] arc, Vector2 center, bool right, int kind, float scale)
    {
        if (arc[arc.Length - 1] == arc[arc.Length - 2]) return;
        float[] angles = new float[arc.Length];
        for (int i = 1; i < arc.Length; i++) angles[i] = (arc[i - 1] - arc[i]).ToRotation();
        angles[0] = angles[1];
        Color theme = kind == 3 ? new Color(69, 64, 91) : kind == 7 ? new Color(165, 185, 205) : BladeRouteRules.Theme(kind);
        Color rim = Color.Lerp(theme, Color.White, .6f);
        VertexStrip shadow = new(), outer = new(), core = new();
        Vector2 offset = center - Main.screenPosition;
        shadow.PrepareStrip(arc, angles, _ => theme * .65f, _ => 35 * scale, offset);
        outer.PrepareStrip(arc, angles, _ => theme, _ => 43 * scale, offset);
        core.PrepareStrip(arc, angles, _ => rim, _ => 36 * scale, offset);
        subtract ??= new BlendState {
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = BlendState.Additive.AlphaSourceBlend,
            AlphaDestinationBlend = BlendState.Additive.AlphaDestinationBlend,
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            ColorSourceBlend = BlendState.Additive.ColorSourceBlend,
            ColorDestinationBlend = BlendState.Additive.ColorDestinationBlend
        };
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try {
            var device = Main.graphics.GraphicsDevice;
            device.BlendState = subtract;
            device.Textures[0] = BladeRouteArt.Read("ArknightsMod/Content/Textures/duaog/wbjex" + (right ? "1" : "2"));
            shadow.DrawTrail(); shadow.DrawTrail(); core.DrawTrail();
            device.BlendState = BlendState.AlphaBlend;
            outer.DrawTrail();
            device.Textures[0] = BladeRouteArt.Read("ArknightsMod/Content/Textures/duaog/wbjex" + (right ? "3" : "4"));
            core.DrawTrail();
        }
        finally {
            Main.spriteBatch.End();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
