using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.Spears;

internal static class VanguardChargeTrail
{
    private static Effect material;
    internal static void Unload() => material = null;
    internal static void Draw(Vector2 head, Vector2[] history, VanguardSpearKind kind, float age)
    {
        if (Main.dedServ || !VanguardSpearVfx.Visible(head)) return;
        var path = new List<Vector2> { head };
        float length = 0;
        foreach (Vector2 point in history)
        {
            float span = Vector2.Distance(path[^1], point);
            if (!float.IsFinite(span) || span > 96 || point == Vector2.Zero) break;
            if (span < .5f) continue;
            if (length + span > 320) { path.Add(Vector2.Lerp(path[^1], point, (320-length)/span)); break; }
            length += span; path.Add(point);
        }
        if (path.Count < 3) return;
        var mesh = new VertexPositionColorTexture[path.Count * 2];
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/VanguardChargeTrail", AssetRequestMode.ImmediateLoad).Value;
        material.Parameters["WorldView"].SetValue(Main.GameViewMatrix.TransformationMatrix
            * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1));
        material.Parameters["FlowTime"].SetValue(age / 8f);
        material.Parameters["BodyTint"].SetValue(VanguardSpearRules.Tint(kind).ToVector3());
        material.Parameters["EdgeTint"].SetValue(VanguardSpearVfx.Accent(kind).ToVector3());
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            device.BlendState = BlendState.AlphaBlend; device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            for (int strand = -1; strand <= 1; strand++)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    float t = i / (float)(path.Count - 1);
                    Vector2 tangent = (path[Math.Max(0,i-1)] - path[Math.Min(path.Count-1,i+1)]).SafeNormalize(Vector2.UnitX);
                    Vector2 normal = tangent.RotatedBy(MathHelper.PiOver2);
                    float taper = MathF.Sin(MathHelper.Pi * (.12f + t * .88f));
                    float width = (strand == 0 ? 22 + (int)kind * 2 : 6) * taper;
                    float offset = strand * (18 + MathF.Sin(t * 13 - age * .4f) * 7) * taper;
                    Vector2 at = path[i] + normal * offset - Main.screenPosition;
                    Color tint = Color.White * (strand == 0 ? .85f : .55f);
                    mesh[i*2] = new(new Vector3(at + normal*width,0),tint,new(t,0));
                    mesh[i*2+1] = new(new Vector3(at - normal*width,0),tint,new(t,1));
                }
                material.CurrentTechnique.Passes[0].Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip,mesh,0,mesh.Length-2);
            }
        }
        finally
        {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,
                DepthStencilState.None,RasterizerState.CullCounterClockwise,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
