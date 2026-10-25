using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal static class MedicPointedTrail
{
    internal static Effect Material;
    internal static float WidthAt(float t,float width)
    {
        t=MathHelper.Clamp(t,0,1);
        return width*MathF.Pow(Math.Max(0,MathF.Sin(MathHelper.Pi*t)),.65f)*MathF.Sqrt(1-t);
    }
    internal static void Draw(Projectile q,MedicalMaterial design,Color body,Color core,float age,float opacity)
    {
        if(Main.dedServ||design.Trail==MedicalTrail.None)return;
        var path=new List<Vector2>(26){q.Center+q.velocity.SafeNormalize(Vector2.UnitX)*12};
        float distance=0;
        foreach(Vector2 old in q.oldPos)
        {
            if(old==Vector2.Zero)break;
            Vector2 at=old+q.Size*.5f;float length=Vector2.Distance(path[^1],at);
            if(!float.IsFinite(length)||length>100)break;
            if(length<1)continue;
            float remaining=design.Length-distance;
            path.Add(length>remaining?Vector2.Lerp(path[^1],at,remaining/length):at);
            distance+=Math.Min(length,remaining);if(distance>=design.Length)break;
        }
        if(path.Count<3||distance<2)return;
        int strands=design.Trail==MedicalTrail.Fork?2:1;
        var vertices=new VertexPositionColorTexture[path.Count*2];
        var device=Main.spriteBatch.GraphicsDevice;
        Material??=ModContent.Request<Effect>("ArknightsMod/Assets/Effects/MedicFlow",AssetRequestMode.ImmediateLoad).Value;
        Main.spriteBatch.End();
        try
        {
            device.BlendState=BlendState.Additive;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            Material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,0,1));
            Material.Parameters["Clock"].SetValue(age/60);Material.Parameters["Pattern"].SetValue(design.Trail==MedicalTrail.Lance?0f:1f);
            Material.Parameters["FlowTexture"].SetValue(MedicArtsVfx.Texture("ArknightsMod/Content/Textures/OperatorVfx/TrailFilament"));
            for(int strand=0;strand<strands;strand++)
            {
                float along=0;
                for(int i=0;i<path.Count;i++)
                {
                    if(i>0)along+=Vector2.Distance(path[i],path[i-1]);
                    float t=MathHelper.Clamp(along/distance,0,1);
                    Vector2 tangent=(path[Math.Max(0,i-1)]-path[Math.Min(path.Count-1,i+1)]).SafeNormalize(Vector2.UnitX);
                    Vector2 side=tangent.RotatedBy(MathHelper.PiOver2);
                    // Head is exactly zero width, broadens behind the nose, then closes to the tail.
                    float w=WidthAt(t,design.Width);
                    Vector2 center=path[i]+side*(strands==2?MathF.Sin(t*8-age*.13f+strand*MathHelper.Pi)*7:0);
                    Color tint=Color.Lerp(core,body,MathHelper.Clamp(t*2,0,1))*(MathF.Pow(1-t,1.2f)*opacity);
                    vertices[i*2]=new(new Vector3(center-side*w-Main.screenPosition,0),tint,new(t,0));
                    vertices[i*2+1]=new(new Vector3(center+side*w-Main.screenPosition,0),tint,new(t,1));
                }
                Material.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleStrip,vertices,0,vertices.Length-2);
            }
        }
        finally {MedicIdentityVisuals.RestoreSprites();}
    }
}
