using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// Subsuming Vortex's SideStreakTrail shader, credited to Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
// Only the historical-path trail is drawn here. No vortex body/noise layers.
internal static class MedicVortexVisuals
{
    internal static Effect Material;
    internal const string NoisePath="Terraria/Images/Misc/Perlin";
    private struct TrailVertex : IVertexType
    {
        public Vector3 Position;
        public Color Color;
        public Vector3 TextureCoordinates;
        private static readonly VertexDeclaration Declaration=new(
            new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),
            new VertexElement(12,VertexElementFormat.Color,VertexElementUsage.Color,0),
            new VertexElement(16,VertexElementFormat.Vector3,VertexElementUsage.TextureCoordinate,0));
        VertexDeclaration IVertexType.VertexDeclaration=>Declaration;
        public TrailVertex(Vector2 point,Color color,float u,float v,float width)
        {Position=new Vector3(point,0);Color=color;TextureCoordinates=new Vector3(u,v,width);}
    }

    internal static void Draw(MedicArt art,float opacity)
    {
        if(Main.dedServ || opacity<=0 || !MedicSpriteFlightVisuals.Spins(art.ProfileId))return;
        var q=art.Projectile;
        var path=new List<Vector2>{q.Center};
        float length=0;
        foreach(Vector2 old in q.oldPos)
        {
            if(old==Vector2.Zero)break;
            Vector2 at=old+q.Size*.5f;
            float distance=Vector2.Distance(at,path[^1]);
            if(!float.IsFinite(distance) || distance>80)break;
            if(distance<.5f)continue;
            path.Add(at);length+=distance;
            if(length>=110)break;
        }
        if(path.Count<3 || length<2)return;
        Material??=ModContent.Request<Effect>("ArknightsMod/Assets/Effects/MedicSideStreak",AssetRequestMode.ImmediateLoad).Value;
        if(!MedicArtsVfx.Materials.TryGetValue(NoisePath,out Texture2D noise) || noise==null)
            MedicArtsVfx.Materials[NoisePath]=noise=ModContent.Request<Texture2D>(NoisePath,AssetRequestMode.ImmediateLoad).Value;
        var vertices=new TrailVertex[path.Count*2];
        float along=0,velocityFade=Utils.GetLerpValue(2,5,q.velocity.Length(),true);
        for(int i=0;i<path.Count;i++)
        {
            if(i>0)along+=Vector2.Distance(path[i-1],path[i]);
            float t=along/length;
            Vector2 tangent=(path[Math.Min(i+1,path.Count-1)]-path[Math.Max(0,i-1)]).SafeNormalize(Vector2.UnitX);
            Vector2 side=tangent.RotatedBy(MathHelper.PiOver2);
            float halfWidth=8f*MathHelper.SmoothStep(.6f,1f,Utils.GetLerpValue(0,.3f,t,true));
            Color color=art.Profile.Body*(opacity*(1-t)*Utils.GetLerpValue(.04f,.2f,t,true)*velocityFade);
            Vector2 center=path[i]-Main.screenPosition;
            vertices[i*2]=new(center-side*halfWidth,color,t,.5f-halfWidth*.5f,halfWidth);
            vertices[i*2+1]=new(center+side*halfWidth,color,t,.5f+halfWidth*.5f,halfWidth);
        }
        var device=Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            device.BlendState=BlendState.Additive;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            Material.Parameters["uWorldViewProjection"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,0,1));
            Material.Parameters["uTime"].SetValue(art.Age/60f);
            Material.CurrentTechnique=Material.Techniques["Technique1"];
            Material.CurrentTechnique.Passes["TrailPass"].Apply();
            device.Textures[1]=noise;device.SamplerStates[1]=SamplerState.LinearWrap;
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip,vertices,0,vertices.Length-2);
        }
        finally {MedicIdentityVisuals.RestoreSprites();}
    }
}
