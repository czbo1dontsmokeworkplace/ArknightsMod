using System;
using System.Collections.Generic;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// A world-distance-limited textured mesh. No high extra-update count or Calamity shader dependency.
internal static class MedicRibbon
{
    private static BasicEffect effect;
    internal static void Draw(Projectile projectile, Color body, Color core, float width, float maxLength, float age)
    {
        var path = new List<Vector2>(25) { projectile.Center };
        float total = 0;
        foreach (Vector2 saved in projectile.oldPos)
        {
            if (saved == Vector2.Zero) break;
            Vector2 next = saved + projectile.Size * .5f;
            float distance = next.Distance(path[^1]);
            if (!float.IsFinite(distance) || distance > 96) break;
            if (distance < .1f) continue;
            float remaining = maxLength - total;
            path.Add(distance > remaining ? Vector2.Lerp(path[^1], next, remaining / distance) : next);
            total += Math.Min(distance, remaining);
            if (total >= maxLength) break;
        }
        if (path.Count < 3) return;
        var vertices = new VertexPositionColorTexture[path.Count * 2];
        float along = 0;
        for (int i = 0; i < path.Count; i++)
        {
            if (i > 0) along += Vector2.Distance(path[i], path[i - 1]);
            float t = along / Math.Max(1, total);
            Vector2 tangent = (path[Math.Max(0, i - 1)] - path[Math.Min(path.Count - 1, i + 1)]).SafeNormalize(Vector2.UnitX);
            Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);
            float envelope = MathF.Sin(MathHelper.Pi * (.16f + t * .84f)) * (1 - t);
            Color color = Color.Lerp(core, body, MathHelper.Clamp(t * 2, 0, 1)) * MathF.Pow(1 - t, 1.4f);
            for (int side = 0; side < 2; side++)
                vertices[i * 2 + side] = new(new Vector3(path[i] + normal * width * envelope * (side == 0 ? -1 : 1) - Main.screenPosition, 0),
                    color, new Vector2(t * 1.5f - age * .025f, side));
        }
        GraphicsDevice device = Main.instance.GraphicsDevice;
        effect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.Additive;
            device.SamplerStates[0] = SamplerState.LinearWrap;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.World = Matrix.Identity;
            effect.View = Main.GameViewMatrix.TransformationMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1);
            effect.Texture = MedicArtsVfx.Texture(OperatorFlightVfx.Assets + "TrailFilament");
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
            }
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static void Unload()
    {
        BasicEffect old = effect; effect = null;
        if (old != null) Main.QueueMainThreadAction(old.Dispose);
        Texture2D oldVeil = MedicArtsVfx.VeilTexture; MedicArtsVfx.VeilTexture = null;
        if (oldVeil != null) Main.QueueMainThreadAction(oldVeil.Dispose);
        MedicArtsVfx.Materials.Clear();
    }
}

public sealed class MedicArtsGraphics : ModSystem
{
    public override void Unload() => MedicRibbon.Unload();
}
