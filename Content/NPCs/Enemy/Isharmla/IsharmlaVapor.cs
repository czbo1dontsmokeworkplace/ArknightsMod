using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public static class IsharmlaVaporRules
{
    public const int Capacity = 200, PerTick = 22, TrailLength = 42;
    public static float Envelope(float age, float life) => MathHelper.SmoothStep(0, 1, Math.Clamp(age / 10, 0, 1)) *
        (1 - MathHelper.SmoothStep(0, 1, Math.Clamp((age / life - .32f) / .68f, 0, 1)));
    public static bool TrailConnects(Vector2 a, Vector2 b) => Vector2.DistanceSquared(a,b) < 170 * 170;
    public static float PlayerClearance(float distance) => MathHelper.Lerp(.30f,1,MathHelper.SmoothStep(0,1,Math.Clamp((distance-50)/120,0,1)));
}

// Cosmetic water volume, independent of encounter RNG, damage and projectile slots.
public sealed class IsharmlaVapor : ModSystem
{
    private struct Puff
    {
        internal Vector2 At, Velocity, Size;
        internal float Rotation, Spin, Strength;
        internal int Age, Life;
    }
    private sealed class Wake
    {
        internal readonly List<Vector2> Points = new(IsharmlaVaporRules.TrailLength);
        internal int Encounter, Seen;
        internal float Opacity;
    }
    private static readonly Puff[] puffs = new Puff[IsharmlaVaporRules.Capacity];
    private static readonly Dictionary<int,Wake> wakes = new();
    private static readonly UnifiedRandom random = new(731984);
    private static readonly List<VertexPositionColorTexture> haze = new(1600), flow = new(5000), film = new(24);
    private static VertexPositionColorTexture[] upload = new VertexPositionColorTexture[8192];
    private static Effect material;
    private static int cursor, tick, emitted;
    private static bool failed;
    private static readonly Color Pearl = new(204,237,243);
    public static int Count { get { int count=0; foreach(var p in puffs) if(p.Life>0) count++; return count; } }
    public static void Clear() { Array.Clear(puffs); wakes.Clear(); cursor=tick=emitted=0; haze.Clear(); flow.Clear(); film.Clear(); }
    public static void Emit(Vector2 at, Vector2 velocity, Vector2 size, float strength=.7f, int life=75, float rotation=0)
    {
        if(Main.dedServ || emitted>=IsharmlaVaporRules.PerTick ||
            !float.IsFinite(at.X+at.Y+velocity.X+velocity.Y+size.X+size.Y+strength+rotation)) return;
        if(Main.LocalPlayer.active && Vector2.DistanceSquared(at,Main.LocalPlayer.Center)>3600*3600) return;
        emitted++;
        puffs[cursor++%puffs.Length]=new Puff { At=at, Velocity=velocity, Size=Vector2.Clamp(size,new Vector2(12),new Vector2(260,130)),
            Strength=Math.Clamp(strength,0,1),Life=Math.Clamp(life,15,130),Rotation=rotation,Spin=random.NextFloat(-.006f,.006f) };
    }
    public override void PostUpdateEverything() => UpdateScene();
    public static void UpdateScene()
    {
        if(Main.dedServ) return;
        if(Main.gameMenu) { Clear();return; }
        tick++;emitted=0;
        for(int i=0;i<puffs.Length;i++)
        {
            ref Puff p=ref puffs[i];if(p.Life<=0)continue;
            if(++p.Age>=p.Life){p.Life=0;continue;}
            p.Velocity*=.974f;p.Velocity.Y-=.009f;p.At+=p.Velocity;p.Rotation+=p.Spin;
            p.Size*=new Vector2(1.004f,1.006f);
        }
        foreach(NPC npc in Main.ActiveNPCs)
        {
            if(npc.ModNPC is Isharmla boss && boss.Initialized && HumanVisibility(boss)>.1f)
            {
                if(tick%9==0)
                {
                    float side=tick%18==0?1:-1;
                    Emit(npc.Center+new Vector2(side*45,35),new Vector2(side*.34f,-.25f),new Vector2(85,32),.25f*HumanVisibility(boss),48,side*.15f);
                }
                if(boss.Transition>0 && tick%5==0)
                    Emit(npc.Center+random.NextVector2CircularEdge(75,60),new Vector2(.3f,-.7f),new Vector2(140,62),.75f,85);
                if(tick%14==0)
                    Emit(new Vector2(npc.Center.X+random.NextFloat(-240,240),boss.Anchor.Y+3),new Vector2(.6f,-.2f),new Vector2(170,35),.52f,105);
            }
            else if(npc.ModNPC is IsharmlaDragon dragon && dragon.Boss!=null)
            {
                if(!wakes.TryGetValue(npc.whoAmI,out Wake w))wakes[npc.whoAmI]=w=new Wake();
                if(w.Encounter!=dragon.Encounter || w.Points.Count>0 && !IsharmlaVaporRules.TrailConnects(w.Points[^1],npc.Center))w.Points.Clear();
                w.Encounter=dragon.Encounter;w.Seen=tick;w.Opacity=dragon.Visibility*(dragon.Dying>0?Math.Max(0,1-dragon.Dying/215f):1);
                if(w.Opacity<.25f)w.Points.Clear();
                else if(w.Points.Count==0 || Vector2.DistanceSquared(w.Points[^1],npc.Center)>9)
                {w.Points.Add(npc.Center);if(w.Points.Count>IsharmlaVaporRules.TrailLength)w.Points.RemoveAt(0);}
                if(tick%5==0 && w.Opacity>.5f)
                {
                    Vector2 normal=dragon.Direction.RotatedBy(MathHelper.PiOver2);
                    for(int side=-1;side<=1;side+=2)Emit(npc.Center-dragon.Direction*70+normal*side*135,
                        -dragon.Direction*.8f+normal*side*.5f,new Vector2(135,60),.7f,65,dragon.FacingAngle);
                }
                if(dragon.PortalOpen && tick%6==0)
                {
                    PortalMist(dragon.PortalEntrance,dragon.PortalIncoming);PortalMist(dragon.PortalExit,dragon.PortalOutgoing);
                }
            }
            else if(npc.ModNPC is IsharmlaSegment part && part.Visibility>.65f && part.Order%3==0 && (tick+part.Order)%12==0 && part.Head is { } head)
            {
                Vector2 forward=(npc.rotation+IsharmlaRules.BodyArtAngle).ToRotationVector2();
                Vector2 normal=forward.RotatedBy(MathHelper.PiOver2);
                for(int side=-1;side<=1;side+=2)Emit(npc.Center+normal*side*110,
                    -forward*.65f+normal*side*.32f,new Vector2(150,65),.52f,66,forward.ToRotation());
            }
        }
        foreach(Projectile p in Main.ActiveProjectiles)
            if(p.ModProjectile is IsharmlaHazard h && h.Boss!=null) HazardMist(h);
        // NPC slots can be reused after death; never connect unrelated encounters.
        if(tick%30==0)
        {
            List<int> retired=new();foreach(var pair in wakes)if(tick-pair.Value.Seen>12)retired.Add(pair.Key);
            foreach(int key in retired)wakes.Remove(key);
        }
    }
    private static void PortalMist(Vector2 center,Vector2 direction)
    {
        Vector2 normal=direction.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        Emit(center+normal*random.NextFloat(-130,130),-direction*.8f+normal*.4f,new Vector2(130,65),.6f,70,direction.ToRotation());
    }
    public static void HazardMist(IsharmlaHazard h)
    {
        Vector2 d=h.Direction.SafeNormalize(Vector2.UnitY),n=d.RotatedBy(MathHelper.PiOver2);
        if(h.Kind is TideShot.Column or TideShot.Breath or TideShot.Gate or TideShot.Lattice)
        {
            bool active=h.Age>=h.Delay;
            if((tick+h.Projectile.whoAmI)%7!=0)return;
            if(!active)
                Emit(h.Projectile.Center+n*random.NextFloat(-45,45),-n*.35f,new Vector2(100,42),.45f,40,d.ToRotation());
            else
            {
                float travel=random.NextFloat(0,Math.Max(0,h.Length));
                Emit(h.Projectile.Center+d*travel+n*random.NextFloat(-h.Width*.45f,h.Width*.45f),
                    d*(h.Kind==TideShot.Breath?2:.7f)+n*random.NextFloat(-.55f,.55f),new Vector2(h.Kind==TideShot.Breath?185:115,46),.60f,55,d.ToRotation());
                if(h.Kind!=TideShot.Breath)Emit(h.Projectile.Center,n*random.NextFloat(-1,1),new Vector2(150,55),.75f,70);
            }
        }
        else if(h.Kind==TideShot.RainCloud && tick%11==0)
            Emit(h.Projectile.Center+new Vector2(random.NextFloat(-h.Width*.3f,h.Width*.3f),4),new Vector2(.3f,-.18f),new Vector2(145,48),.68f,65);
        else if(h.Kind==TideShot.Wave && tick%6==0)
            Emit(h.Projectile.Center-new Vector2(0,45),new Vector2(-Math.Sign(d.X)*.8f,-.4f),new Vector2(115,70),.65f,62);
    }
    public override void PostDrawTiles()
    {
        if(Main.dedServ||Main.gameMenu)return;
        bool present=Count>0;
        if(!present)foreach(NPC npc in Main.ActiveNPCs)if(npc.ModNPC is Isharmla or IsharmlaDragon){present=true;break;}
        if(!present)return;
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,
            RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        try { DrawScene(Main.spriteBatch); } finally { Main.spriteBatch.End(); }
    }
    public static void DrawScene(SpriteBatch batch)
    {
        haze.Clear();flow.Clear();film.Clear();
        foreach(Puff p in puffs)if(p.Life>0)
        {
            float clear=IsharmlaVaporRules.PlayerClearance(Vector2.Distance(p.At,Main.LocalPlayer.Center));
            Quad(haze,p.At,p.Size,p.Rotation,Pearl*(p.Strength*clear*IsharmlaVaporRules.Envelope(p.Age,p.Life)));
        }
        foreach(NPC npc in Main.ActiveNPCs)
        {
            if(npc.ModNPC is Isharmla b && b.Initialized)HumanCurrents(b.NPC.Center,HumanVisibility(b)*(.14f+b.Gesture*.18f));
            if(npc.ModNPC is IsharmlaDragon d && wakes.TryGetValue(npc.whoAmI,out Wake w) && tick-w.Seen<3)DrawWake(w,d);
        }
        Flush(batch);
    }
    private static float HumanVisibility(Isharmla boss)
    {
        float opacity = (boss.Stage is TideStage.Covenant or TideStage.LastTide) && boss.Dragon is { } mount ? mount.Visibility : 1;
        return boss.Stage == TideStage.Finale ? opacity * IsharmlaRules.FinaleOpacity(boss.Timer, boss.Transition) : opacity;
    }
    private static void HumanCurrents(Vector2 center,float opacity)
    {
        if(opacity<=.001f)return;
        float time=IsharmlaVisuals.Clock;
        // Open, gently offset currents leave gaps around the silhouette.
        for(int layer=0;layer<2;layer++)
        {
            Vector2[] path=new Vector2[41];
            for(int i=0;i<path.Length;i++)
            {
                float a=i/(float)(path.Length-1)*MathHelper.Pi*1.48f+time*.18f+layer*2.1f;
                path[i]=center+new Vector2(MathF.Cos(a)*(88+layer*19),MathF.Sin(a)*(24+layer*10)+39-layer*27+MathF.Sin(a*2+time*.7f)*8);
            }
            Ribbon(flow,path,layer==0?26:20,Pearl*(opacity*(layer==0?.70f:.45f)));
        }
        Quad(haze,center+new Vector2(-63,21),new Vector2(180,100),-.25f,Pearl*(.44f*opacity));
        Quad(haze,center+new Vector2(64,7),new Vector2(160,90),.30f,Pearl*(.4f*opacity));
    }
    public static void DrawRayVeil(SpriteBatch batch,Vector2 origin,Vector2 direction,float length,float width,float opacity)
    {
        haze.Clear();flow.Clear();film.Clear();
        Vector2 axis=direction.SafeNormalize(Vector2.UnitY),normal=axis.RotatedBy(MathHelper.PiOver2);
        Vector2[] path=new Vector2[33];
        for(int i=0;i<path.Length;i++)
        {
            float u=i/32f;
            path[i]=origin+axis*(u*length)+normal*(MathF.Sin(u*19-IsharmlaVisuals.Clock*2)*width*.09f);
        }
        Ribbon(flow,path,width*1.35f,Pearl*(opacity*.42f));
        Flush(batch);
    }
    private static void DrawWake(Wake wake,IsharmlaDragon dragon)
    {
        if(wake.Points.Count<3)return;
        for(int side=-1;side<=1;side+=2)
        {
            Vector2[] path=new Vector2[wake.Points.Count];
            for(int i=0;i<path.Length;i++)
            {
                Vector2 direction=(wake.Points[Math.Min(path.Length-1,i+1)]-wake.Points[Math.Max(0,i-1)]).SafeNormalize(dragon.Direction);
                float u=i/(float)(path.Length-1);
                path[i]=wake.Points[i]+direction.RotatedBy(MathHelper.PiOver2)*(side*(135+MathF.Sin(u*4+IsharmlaVisuals.Clock)*12));
            }
            Ribbon(flow,path,50,Pearl*(wake.Opacity*.58f));
        }
    }
    public static void DrawHumanFilm(SpriteBatch batch,Vector2 center,float opacity,bool foreground=false)
    {
        haze.Clear();flow.Clear();film.Clear();
        if(!foreground)Quad(film,center,new Vector2(174,184),0,Pearl*(opacity*.88f));
        else
        {
            Vector2[] path=new Vector2[25];
            for(int i=0;i<path.Length;i++)
            {
                float a=MathHelper.Lerp(.12f,MathHelper.Pi-.12f,i/24f);
                path[i]=center+new Vector2(MathF.Cos(a)*89,MathF.Sin(a)*25+43+MathF.Sin(IsharmlaVisuals.Clock*.8f+a)*4);
            }
            Ribbon(flow,path,16,Pearl*(opacity*.7f));
        }
        Flush(batch);
    }
    private static void Quad(List<VertexPositionColorTexture> vertices,Vector2 center,Vector2 size,float rotation,Color color)
    {
        Vector2 x=rotation.ToRotationVector2()*size.X*.5f,y=x.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2)*size.Y*.5f;
        Add(vertices,center-x-y,color,new(0,0));Add(vertices,center-x+y,color,new(0,1));Add(vertices,center+x-y,color,new(1,0));
        Add(vertices,center+x-y,color,new(1,0));Add(vertices,center-x+y,color,new(0,1));Add(vertices,center+x+y,color,new(1,1));
    }
    private static void Ribbon(List<VertexPositionColorTexture> vertices,Vector2[] path,float width,Color color)
    {
        for(int i=0;i<path.Length-1;i++)
        {
            Vector2 n=(path[i+1]-path[Math.Max(0,i-1)]).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Vector2 next=(path[Math.Min(path.Length-1,i+2)]-path[i]).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float a=i/(float)(path.Length-1),b=(i+1)/(float)(path.Length-1);
            Vector2 w=n*width, wn=next*width;
            Add(vertices,path[i]-w,color,new(a,0));Add(vertices,path[i]+w,color,new(a,1));Add(vertices,path[i+1]-wn,color,new(b,0));
            Add(vertices,path[i+1]-wn,color,new(b,0));Add(vertices,path[i]+w,color,new(a,1));Add(vertices,path[i+1]+wn,color,new(b,1));
        }
    }
    private static void Add(List<VertexPositionColorTexture> v,Vector2 p,Color c,Vector2 uv)=>v.Add(new(new Vector3(p-Main.screenPosition,0),c,uv));
    private static void EnsureMaterial()
    {
        if(material!=null||failed)return;
        try { material=new Effect(Main.instance.GraphicsDevice,ModContent.GetInstance<IsharmlaVapor>().Mod.GetFileBytes("Assets/Effects/IsharmlaVapor.fxc")); }
        catch(Exception e){failed=true;ModContent.GetInstance<IsharmlaVapor>().Mod.Logger.Warn("Tidal vapor unavailable: "+e.Message);}
    }
    private static void Flush(SpriteBatch batch)
    {
        if(Main.dedServ||haze.Count+flow.Count+film.Count==0)return;EnsureMaterial();if(material==null)return;
        GraphicsDevice device=batch.GraphicsDevice;
        batch.End();
        try
        {
            device.BlendState=BlendState.AlphaBlend;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            material.Parameters["uTransform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,0,1));
            material.Parameters["uTime"].SetValue(IsharmlaVisuals.Clock);
            Draw(haze,"Vapor");Draw(flow,"Current");Draw(film,"Film");
        }
        finally
        {
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,
                RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        }
        void Draw(List<VertexPositionColorTexture> list,string technique)
        {
            if(list.Count==0)return;if(upload.Length<list.Count)Array.Resize(ref upload,list.Count*2);list.CopyTo(upload);
            material.CurrentTechnique=material.Techniques[technique];material.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList,upload,0,list.Count/3);
        }
    }
    public override void OnWorldUnload()=>Clear();
    public override void Unload()
    {
        Clear();Effect old=material;material=null;failed=false;if(!Main.dedServ)Main.QueueMainThreadAction(()=>old?.Dispose());
    }
}
