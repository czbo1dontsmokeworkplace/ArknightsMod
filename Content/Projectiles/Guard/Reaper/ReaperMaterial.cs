using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

internal static class ReaperMaterial
{
    private static Effect material;
    private static Effect legacyWave;
    private static readonly VertexPositionTexture[] legacyQuad = new VertexPositionTexture[4];
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[100];
    internal static void Unload() { material = legacyWave = null; }
    internal static void LegacyCrescent(Vector2 center,float angle,float half,float opacity,float flatten,bool grayscale=false)
    {
        if(!ReaperVisuals.Visible(center)||opacity<=.001f)return;
        // Reuse the existing Entelechia procedural crescent and its layered tails.
        legacyWave ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/EntelechiaBladeWave",AssetRequestMode.ImmediateLoad).Value;
        legacyWave.Parameters["MatrixTransform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,Main.screenWidth,Main.screenHeight,0,-1,1));
        legacyWave.Parameters["uGrayscale"].SetValue(grayscale?1f:0f);
        legacyWave.Parameters["uColor"].SetValue(new Vector3(.56f,.60f,.66f));
        legacyWave.Parameters["uSecondaryColor"].SetValue(new Vector3(.97f,.98f,1));
        legacyWave.Parameters["uProgress"].SetValue(opacity);legacyWave.Parameters["uRedProgress"].SetValue(opacity*.85f);
        legacyWave.Parameters["uIntensity"].SetValue(Main.dayTime?1.15f:.9f);legacyWave.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly*3);
        Vector2 x=angle.ToRotationVector2()*half,y=(angle+MathHelper.PiOver2).ToRotationVector2()*half*flatten;
        legacyQuad[0]=new(new Vector3(center-x-y-Main.screenPosition,0),new(0,0));legacyQuad[1]=new(new Vector3(center+x-y-Main.screenPosition,0),new(1,0));
        legacyQuad[2]=new(new Vector3(center-x+y-Main.screenPosition,0),new(0,1));legacyQuad[3]=new(new Vector3(center+x+y-Main.screenPosition,0),new(1,1));
        var device=Main.spriteBatch.GraphicsDevice;Main.spriteBatch.End();
        try {device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            foreach(var pass in legacyWave.CurrentTechnique.Passes){pass.Apply();device.DrawUserPrimitives(PrimitiveType.TriangleStrip,legacyQuad,0,2);}}
        finally {Main.pixelShader.CurrentTechnique.Passes[0].Apply();Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);}
    }
    internal static void Trail(Vector2 center, Vector2[] history, int samples, ReaperKind kind, float opacity)
    {
        if (!ReaperVisuals.Visible(center) || samples < 2) return;
        for (int i = 0; i < samples; i++) {
            if (!float.IsFinite(history[i].X) || !float.IsFinite(history[i].Y) || Vector2.DistanceSquared(center,history[i]) > 500*500) return;
        }
        int count = Math.Min(50, (samples - 1) * 3 + 1);
        for (int i = 0; i < count; i++) {
            float sample = i * (samples - 1f) / (count - 1), blend = sample % 1;
            int current = (int)sample, next = Math.Min(current + 1, samples - 1);
            Vector2 a = history[current] - center, b = history[next] - center;
            float angle = a.ToRotation() + MathHelper.WrapAngle(b.ToRotation() - a.ToRotation()) * blend;
            Vector2 outer = center + angle.ToRotationVector2() * MathHelper.Lerp(a.Length(), b.Length(), blend);
            Vector2 inner = Vector2.Lerp(center,outer,.82f);
            vertices[i*2] = new(new Vector3(outer-Main.screenPosition,0),Color.White,new(i/(float)(count-1),0));
            vertices[i*2+1] = new(new Vector3(inner-Main.screenPosition,0),Color.White,new(i/(float)(count-1),1));
        }
        Draw(count*2,kind,opacity);
    }
    internal static void Crescent(Vector2 center,float angle,float radius,ReaperKind kind,float opacity)
    {
        if (!ReaperVisuals.Visible(center) || radius <= 0 || radius > 320) return;
        for(int i=0;i<50;i++) {
            float t=i/49f,rotation=angle-t*3.7f;
            Vector2 ray=rotation.ToRotationVector2(); float width=MathF.Sin(t*MathHelper.Pi)*radius*.16f;
            vertices[i*2]=new(new Vector3(center+ray*radius-Main.screenPosition,0),Color.White,new(t,0));
            vertices[i*2+1]=new(new Vector3(center+ray*(radius-width)-Main.screenPosition,0),Color.White,new(t,1));
        }
        Draw(100,kind,opacity);
    }
    private static void Draw(int count,ReaperKind kind,float opacity)
    {
        material ??= ModContent.Request<Effect>("ArknightsMod/Assets/Effects/ReaperSilk",AssetRequestMode.ImmediateLoad).Value;
        material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,Main.screenWidth,Main.screenHeight,0,0,1));
        material.Parameters["Pigment"].SetValue(ReaperRules.Color(kind).ToVector3());material.Parameters["Highlight"].SetValue(ReaperRules.Edge(kind).ToVector3());
        material.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);material.Parameters["Opacity"].SetValue(Math.Clamp(opacity,0,1));
        var device=Main.spriteBatch.GraphicsDevice;Main.spriteBatch.End();
        try {
            device.BlendState=BlendState.NonPremultiplied;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            material.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleStrip,vertices,0,count-2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,
                DepthStencilState.None,RasterizerState.CullCounterClockwise,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
public sealed class ReaperMaterialSystem : ModSystem { public override void Unload() { ReaperMaterial.Unload(); ReaperWispMaterial.Unload(); ReaperVisuals.Unload(); } }
