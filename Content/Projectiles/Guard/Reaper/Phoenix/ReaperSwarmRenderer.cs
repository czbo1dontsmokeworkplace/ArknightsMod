using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

[StructLayout(LayoutKind.Sequential)]
internal struct ReaperSwarmVertex : IVertexType
{
    internal Vector3 Position;
    internal Vector4 Tint;
    internal Vector2 UV;
    internal Vector2 Radiance;
    private static readonly VertexDeclaration declaration=new(
        new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),
        new VertexElement(12,VertexElementFormat.Vector4,VertexElementUsage.Color,0),
        new VertexElement(28,VertexElementFormat.Vector2,VertexElementUsage.TextureCoordinate,0),
        new VertexElement(36,VertexElementFormat.Vector2,VertexElementUsage.TextureCoordinate,1));
    public VertexDeclaration VertexDeclaration=>declaration;
    internal ReaperSwarmVertex(Vector2 p,Vector2 uv,Vector4 tint,float bloom,float lensflare=0)
    {Position=new(p,0);UV=uv;Tint=tint;Radiance=new(bloom,lensflare);}
}

public sealed class ReaperSwarmRenderer : ModSystem
{
    private readonly record struct Command(string Asset,Vector2 A,Vector2 B,Vector2 C,Vector2 D,Vector4 Color,float Bloom,bool Additive,float Z,Vector2 UV0,Vector2 UV1,float Lensflare=0,bool HideBase=false);
    private readonly List<Command> commands=new();
    private readonly List<Command> radianceCommands=new();
    private static readonly Comparison<Command> byTexture=(a,b)=>StringComparer.Ordinal.Compare(a.Asset,b.Asset);
    private static readonly IComparer<Command> textureComparer=Comparer<Command>.Create(byTexture);
    internal int LastCommandCount=>commands.Count;
    internal int LastDrawCalls {get;private set;}
    private readonly Dictionary<string,Texture2D> textures=new(StringComparer.Ordinal);
    private readonly List<Texture2D> ownedTextures=new();
    private readonly ReaperSwarmVertex[] quadVertices=new ReaperSwarmVertex[6];
    private readonly ReaperSwarmVertex[] batchVertices=new ReaperSwarmVertex[24576];
    private Effect effect;
    private RenderTarget2D emission,quarterA,quarterB,eighthA,eighthB,flare;
    private bool hasFlare;
    private bool prepared,failed;
    private int width,height;
    // Both passes receive float4 colors; no 8-bit Color conversion clips HDR RGB.
    private static readonly BlendState add=new(){ColorSourceBlend=Blend.One,ColorDestinationBlend=Blend.One,AlphaSourceBlend=Blend.Zero,AlphaDestinationBlend=Blend.One};
    public override void Load()
    {
        if(Main.dedServ)return;
        On_Main.CheckMonoliths+=PrepareTargets;
    }
    public override void Unload()
    {
        if(!Main.dedServ)On_Main.CheckMonoliths-=PrepareTargets;
        Effect old=effect;effect=null;var targets=new[]{emission,quarterA,quarterB,eighthA,eighthB,flare};
        emission=quarterA=quarterB=eighthA=eighthB=flare=null;
        var tinted=ownedTextures.ToArray();ownedTextures.Clear();
        textures.Clear();commands.Clear();radianceCommands.Clear();prepared=failed=false;
        Main.QueueMainThreadAction(()=>{old?.Dispose();foreach(var t in targets)t?.Dispose();foreach(var t in tinted)t.Dispose();});
    }
    private void PrepareTargets(On_Main.orig_CheckMonoliths orig)
    {
        orig();
        prepared=false;
        if(Main.gameMenu || Main.dedServ || failed || ReaperSwarmVisuals.Nodes.Count==0)return;
        GraphicsDevice g=Main.instance.GraphicsDevice;
        commands.Clear();foreach(var n in ReaperSwarmVisuals.Nodes)n.Collect(this);
        if(commands.Count==0)return;
        commands.Sort((a,b)=>a.Z.CompareTo(b.Z));
        // Addition is commutative. Reorder only uninterrupted additive spans in
        // the base pass, preserving every alpha layer's position in the stack.
        for(int i=0;i<commands.Count;)
        {
            if(!commands[i].Additive){i++;continue;}
            int start=i;while(i<commands.Count && commands[i].Additive)i++;
            commands.Sort(start,i-start,textureComparer);
        }
        radianceCommands.Clear();
        foreach(var c in commands)if(c.Bloom>0 || c.Lensflare>0)radianceCommands.Add(c);
        radianceCommands.Sort(byTexture);LastDrawCalls=0;
        try
        {
            effect??=new Effect(g,Mod.GetFileBytes("Content/Projectiles/Guard/Reaper/Phoenix/Assets/ReaperMaterial.fxc"));
            if(emission==null || emission.IsDisposed || width!=Main.screenWidth || height!=Main.screenHeight)
            {
                emission?.Dispose();quarterA?.Dispose();quarterB?.Dispose();eighthA?.Dispose();eighthB?.Dispose();flare?.Dispose();
                width=Main.screenWidth;height=Main.screenHeight;
                emission=Target(g,width,height);quarterA=Target(g,Math.Max(1,width/4),Math.Max(1,height/4));quarterB=Target(g,quarterA.Width,quarterA.Height);
                eighthA=Target(g,Math.Max(1,width/8),Math.Max(1,height/8));eighthB=Target(g,eighthA.Width,eighthA.Height);
                flare=Target(g,quarterA.Width,quarterA.Height);
            }
            var state=new DeviceState(g);
            try
            {
                g.SetRenderTarget(emission);g.Clear(Color.Transparent);
                DrawCommands(g,1);
                // Filter before reducing resolution; sparse wide blur taps copy
                // narrow source sprites into separate, regularly spaced images.
                Downsample(g,emission,quarterA,"Downsample4");
                Blur(g,quarterA,quarterB,new Vector2(1f/quarterA.Width,0));
                Blur(g,quarterB,quarterA,new Vector2(0,1f/quarterB.Height));
                (quarterA,quarterB)=(quarterB,quarterA);
                Downsample(g,quarterB,eighthA,"Downsample2");
                Blur(g,eighthA,eighthB,new Vector2(1f/eighthA.Width,0));
                Blur(g,eighthB,eighthA,new Vector2(0,1f/eighthB.Height));
                (eighthA,eighthB)=(eighthB,eighthA);
                hasFlare=commands.Any(c=>c.Lensflare>0);
                if(hasFlare)
                {
                    g.SetRenderTarget(emission);g.Clear(Color.Transparent);DrawCommands(g,2);
                    Downsample(g,emission,quarterA,"Downsample4");
                    g.SetRenderTarget(flare);g.Clear(Color.Transparent);g.BlendState=BlendState.Opaque;
                    FullScreen(g,quarterA,"Flare",step:new Vector2(1f/quarterA.Width,0));
                }
                prepared=true;
            }
            finally {state.Restore(g);}
        }
        catch(Exception e)
        {
            failed=true;Mod.Logger.Error("ReaperSwarm HDR material preparation failed; the port is not visually verified.",e);
        }
    }
    private static RenderTarget2D Target(GraphicsDevice g,int w,int h)=>new(g,w,h,false,SurfaceFormat.HalfVector4,DepthFormat.None,0,RenderTargetUsage.DiscardContents);
    public override void PostDrawTiles()
    {
        if(!prepared || effect==null || Main.dedServ)return;
        GraphicsDevice g=Main.instance.GraphicsDevice;
        var state=new DeviceState(g);
        try
        {
            DrawCommands(g,0);
            Composite(g,quarterB,.65f);
            Composite(g,eighthB,.35f);
            if(hasFlare)Composite(g,flare,1);
        }
        finally {state.Restore(g);}
    }
    private Texture2D Texture(string path)
    {
        if(textures.TryGetValue(path,out var t))return t;
        const string monochrome="#monochrome";
        if(path.EndsWith(monochrome,StringComparison.Ordinal))
        {
            // Imported source sprites can contain a baked blue/pink hue. Keep
            // alpha and HDR modulation, but neutralize that hue once per asset
            // so each operator's requested palette remains authoritative.
            Texture2D source=Texture(path[..^monochrome.Length]);
            var pixels=new Color[source.Width*source.Height];source.GetData(pixels);
            for(int i=0;i<pixels.Length;i++)
            {
                byte peak=Math.Max(pixels[i].R,Math.Max(pixels[i].G,pixels[i].B));
                pixels[i]=new Color(peak,peak,peak,pixels[i].A);
            }
            t=new Texture2D(Main.instance.GraphicsDevice,source.Width,source.Height);t.SetData(pixels);ownedTextures.Add(t);
        }
        else t=ModContent.Request<Texture2D>(path,ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        textures.Add(path,t);return t;
    }
    internal void Quad(string asset,Vector2 position,Vector2 size,Vector2 direction,Vector4 color,float bloom,bool additive,float z,float lensflare=0,bool hideBase=false)
    {
        if(string.IsNullOrEmpty(asset) || size.X==0 || size.Y==0 || color.W<=0)return;
        Vector2 right=new Vector2(-direction.Y,direction.X)*size.X*.5f,forward=direction*size.Y*.5f;
        commands.Add(new(asset,position-right+forward,position+right+forward,position+right-forward,position-right-forward,color,bloom,additive,z,Vector2.Zero,Vector2.One,lensflare,hideBase));
    }
    internal void Geometry(string asset,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Vector4 color,float bloom,bool additive,float z)
    {
        if(string.IsNullOrEmpty(asset))return;
        commands.Add(new(asset,a,b,c,d,color,bloom,additive,z,Vector2.Zero,Vector2.One));
    }
    internal void Ribbon(string asset,Vector2 from,Vector2 to,float width,Vector4 color,float bloom,bool additive,float z,float v0,float v1)
    {
        if(string.IsNullOrEmpty(asset))return;
        Vector2 direction=(to-from).Unit(),normal=new Vector2(direction.Y,-direction.X)*width*.5f;
        commands.Add(new(asset,from-normal,from+normal,to+normal,to-normal,color,bloom,additive,z,new(0,v0),new(1,v1)));
    }
    private void DrawCommands(GraphicsDevice g,int pass)
    {
        effect.CurrentTechnique=effect.Techniques["Material"];
        effect.Parameters["uTransform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,width,height,0,-1,1));
        effect.Parameters["uEmission"].SetValue((float)pass);
        g.DepthStencilState=DepthStencilState.None;g.RasterizerState=RasterizerState.CullNone;g.SamplerStates[0]=SamplerState.LinearClamp;
        string asset=null;bool additive=false;int count=0;
        void Flush()
        {
            if(count==0)return;
            g.BlendState=additive?add:BlendState.AlphaBlend;
            effect.Parameters["uTexture"].SetValue(Texture(asset));effect.CurrentTechnique.Passes[0].Apply();
            g.DrawUserPrimitives(PrimitiveType.TriangleList,batchVertices,0,count/3);count=0;LastDrawCalls++;
        }
        foreach(var c in pass==0?commands:radianceCommands)
        {
            if(pass==0 && c.HideBase || pass==1 && c.Bloom<=0 || pass==2 && c.Lensflare<=0)continue;
            bool mode=pass>0 || c.Additive;
            if(asset!=c.Asset || additive!=mode || count+6>batchVertices.Length)Flush();
            asset=c.Asset;additive=mode;
            Fill(c.A-Main.screenPosition,c.B-Main.screenPosition,c.C-Main.screenPosition,c.D-Main.screenPosition,c.UV0,c.UV1,c.Color,c.Bloom,c.Lensflare);
            System.Array.Copy(quadVertices,0,batchVertices,count,6);count+=6;
        }
        Flush();
    }
    private void Fill(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Vector2 uv0,Vector2 uv1,Vector4 color,float bloom,float lensflare=0)
    {
        quadVertices[0]=new(a,uv0,color,bloom,lensflare);quadVertices[1]=new(b,new Vector2(uv1.X,uv0.Y),color,bloom,lensflare);quadVertices[2]=new(c,uv1,color,bloom,lensflare);
        quadVertices[3]=quadVertices[0];quadVertices[4]=quadVertices[2];quadVertices[5]=new(d,new Vector2(uv0.X,uv1.Y),color,bloom,lensflare);
    }
    private void FullScreen(GraphicsDevice g,Texture2D input,string technique,float gain=1,Vector2 step=default)
    {
        effect.CurrentTechnique=effect.Techniques[technique];
        effect.Parameters["uTransform"].SetValue(Matrix.CreateOrthographicOffCenter(0,1,1,0,-1,1));
        effect.Parameters["uTexture"].SetValue(input);effect.Parameters["uTexelStep"].SetValue(step);effect.Parameters["uCompositeGain"].SetValue(gain);
        Fill(Vector2.Zero,Vector2.UnitX,Vector2.One,Vector2.UnitY,Vector2.Zero,Vector2.One,Vector4.One,0);
        effect.CurrentTechnique.Passes[0].Apply();g.DrawUserPrimitives(PrimitiveType.TriangleList,quadVertices,0,2);
    }
    private void Blur(GraphicsDevice g,Texture2D input,RenderTarget2D output,Vector2 step)
    {
        g.SetRenderTarget(output);g.Clear(Color.Transparent);g.BlendState=BlendState.Opaque;
        FullScreen(g,input,"Blur",step:step);
    }
    private void Downsample(GraphicsDevice g,Texture2D input,RenderTarget2D output,string technique)
    {
        g.SetRenderTarget(output);g.Clear(Color.Transparent);g.BlendState=BlendState.Opaque;
        FullScreen(g,input,technique,step:new Vector2(1f/input.Width,1f/input.Height));
    }
    private void Composite(GraphicsDevice g,Texture2D input,float gain)
    {g.BlendState=add;FullScreen(g,input,"Copy",gain);}
    private sealed class DeviceState
    {
        private readonly RenderTargetBinding[] targets;
        private readonly Viewport viewport;
        private readonly Rectangle scissor;
        private readonly BlendState blend;
        private readonly DepthStencilState depth;
        private readonly RasterizerState raster;
        private readonly SamplerState sampler;
        private readonly Texture texture;
        private readonly IndexBuffer indices;
        private readonly VertexBufferBinding[] vertices;
        internal DeviceState(GraphicsDevice g)
        {targets=g.GetRenderTargets();viewport=g.Viewport;scissor=g.ScissorRectangle;blend=g.BlendState;depth=g.DepthStencilState;raster=g.RasterizerState;sampler=g.SamplerStates[0];texture=g.Textures[0];indices=g.Indices;vertices=g.GetVertexBuffers();}
        internal void Restore(GraphicsDevice g)
        {
            if(targets.Length==0)g.SetRenderTarget(null);else g.SetRenderTargets(targets);
            g.Viewport=viewport;g.ScissorRectangle=scissor;g.BlendState=blend;g.DepthStencilState=depth;g.RasterizerState=raster;g.SamplerStates[0]=sampler;g.Textures[0]=texture;g.Indices=indices;
            g.SetVertexBuffers(vertices);
        }
    }
}
