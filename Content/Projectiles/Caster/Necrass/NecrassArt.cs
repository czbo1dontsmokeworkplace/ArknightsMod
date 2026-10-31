using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

// Shared by live drawing and the offline renderer. No gameplay state or draw-state changes.
internal static class NecrassArt
{
    internal const string Assets = "ArknightsMod/Content/Textures/Necrass/";
    internal static readonly Color Violet = new(83, 35, 132), Lilac = new(116, 66, 166), Ash = new(45, 31, 63);
    internal static Texture2D Head, Ribbon, Veil, Rupture, Bloom, Shard, Diadem, Slash;
    internal static bool Ready => Head != null && !Head.IsDisposed;
    internal static void Assign(Func<string, Texture2D> load)
    {
        Head=load("SoulHead");Ribbon=load("SoulRibbon");Veil=load("AshVeil");Rupture=load("SoulRupture");
        Bloom=load("SoulBloom");Shard=load("EmberShard");Diadem=load("SpiritCrown");Slash=load("SpiritSlash");
    }
    internal static Color Glow(Color color,float opacity) => new Color(color.R,color.G,color.B,0)*Math.Clamp(opacity,0,1);
    private static Vector2 Unit(Vector2 v) => v.LengthSquared()>.0001f?Vector2.Normalize(v):Vector2.UnitX;
    private static float Angle(Vector2 v)=>MathF.Atan2(v.Y,v.X);
    private static void Stamp(SpriteBatch b,Texture2D texture,Vector2 at,Vector2 size,float rotation,Color color,Rectangle? frame=null)
    {
        Rectangle rect=frame??new Rectangle(0,0,texture.Width,texture.Height);
        b.Draw(texture,at,rect,color,rotation,new Vector2(rect.Width,rect.Height)*.5f,size/new Vector2(rect.Width,rect.Height),SpriteEffects.None,0);
    }
    private static Rectangle Frame(Texture2D t,int height,int frame)=>new(0,Math.Abs(frame%(t.Height/height))*height,t.Width,height);
    internal static void Flame(SpriteBatch b,Vector2 at,Vector2 direction,float size,float time,int seed,float opacity=1)
    {
        float rotation=Angle(direction);var frame=Frame(Head,128,(int)(time*18)+seed);Vector2 scale=new(size*1.65f,size);
        Stamp(b,Head,at,scale*1.25f,rotation,new Color(24,13,39,210)*(.32f*opacity),frame);
        Stamp(b,Head,at,scale,rotation,Glow(Violet,opacity*.9f),frame);
        Stamp(b,Head,at+Unit(direction)*size*.12f,scale*new Vector2(.82f,.60f),rotation,Glow(Lilac,opacity*.62f),frame);
        Stamp(b,Bloom,at,new Vector2(size*1.45f),0,Glow(Violet,.23f*opacity));
    }
    internal static void Bolt(SpriteBatch b,Vector2 center,Vector2 velocity,Vector2[] history,Vector2 offset,float time,int seed,int mode)
    {
        float limit=mode==2?166:mode==1?180:146;
        Span<Vector2> path=stackalloc Vector2[40];Span<float> distance=stackalloc float[40];path[0]=center;int count=1;float total=0;
        foreach(var old in history)
        {
            if(old==Vector2.Zero||count>=path.Length)break;Vector2 next=old+offset;float step=Vector2.Distance(path[count-1],next);
            if(!float.IsFinite(step)||step>96)break;if(step<.1f)continue;
            if(total+step>limit){next=Vector2.Lerp(path[count-1],next,(limit-total)/step);step=limit-total;}
            total+=step;distance[count]=total;path[count++]=next;if(total>=limit)break;
        }
        float width=mode==1?27:mode==2?20:23;int frame=(Math.Abs(seed)+(int)(time*16))%4;
        if(total>1)for(int layer=0;layer<3;layer++)for(int i=1;i<count;i++)
        {
            Vector2 delta=path[i]-path[i-1];float span=delta.Length();if(span<.1f)continue;
            int from=(int)(distance[i-1]/total*(Ribbon.Width-1)),to=Math.Max(from+1,(int)(distance[i]/total*(Ribbon.Width-1)));
            var source=new Rectangle(from,frame*64,Math.Min(to-from,Ribbon.Width-from),64);
            Color color=layer==0?new Color(32,16,51,185)*.42f:Glow(layer==1?Violet:Lilac,layer==1?.68f:.48f);
            float height=width*(layer==0?1.35f:layer==1?1:.28f);
            b.Draw(Ribbon,path[i-1],source,color,Angle(delta),new Vector2(0,32),new Vector2(span/source.Width,height/64),SpriteEffects.None,0);
        }
        Flame(b,center,velocity,mode==1?27:mode==2?20:23,time,seed);
    }
    internal static void Ground(SpriteBatch b,Vector2 at,float radius,float time,float opacity)
    {
        if(radius<=0||opacity<=0)return;
        var frame=Frame(Veil,160,(int)(time*4));
        Stamp(b,Veil,at,new Vector2(radius*2.05f,radius*.55f),0,new Color(34,18,46,190)*opacity*.45f,frame);
        for(int i=0;i<3;i++)
        {
            float phase=time*.7f+i*2.2f;Vector2 drift=new(MathF.Cos(phase)*radius*.37f,MathF.Sin(phase)*radius*.07f);
            Stamp(b,Veil,at+drift,new Vector2(radius*1.55f,radius*.36f),MathF.Sin(phase)*.10f,Glow(Violet,opacity*.19f),Frame(Veil,160,i));
        }
        Stamp(b,Bloom,at,new Vector2(radius*1.45f,radius*.37f),0,Glow(Lilac,opacity*.12f));
    }
    internal static void Crown(SpriteBatch b,Vector2 at,float radius,float time,float opacity,int jewels)
    {
        at.Y+=MathF.Sin(time*2)*2;
        Stamp(b,Bloom,at,new Vector2(radius*2.6f,radius*1.5f),0,Glow(Violet,opacity*.25f));
        Stamp(b,Diadem,at,new Vector2(radius*2,radius),0,Glow(Violet,opacity*.9f));
        Stamp(b,Diadem,at+new Vector2(0,1),new Vector2(radius*1.85f,radius*.64f),0,Glow(Lilac,opacity*.55f));
        int count=Math.Clamp(jewels,0,6);
        for(int i=0;i<count;i++)
        {
            Vector2 pip=at+new Vector2((i-(count-1)*.5f)*5,radius*.37f+4);
            Stamp(b,Head,pip,new Vector2(6,4),-MathHelper.PiOver2,Glow(Lilac,opacity*.8f),Frame(Head,128,i));
        }
    }
    internal static void Impact(SpriteBatch b,Vector2 at,Vector2 direction,float progress,float radius,int mode)
    {
        progress=Math.Clamp(progress,0,1);float fade=MathF.Pow(1-progress,1.6f),expansion=1-MathF.Pow(1-progress,3);
        if(mode==2)
        {
            Vector2 axis=Unit(direction);float rotation=Angle(axis)+(progress-.15f)*.34f;Vector2 p=at+axis*radius*.53f;
            Stamp(b,Slash,p,new Vector2(radius*1.05f,radius*1.48f),rotation,Glow(Violet,fade*.85f));
            Stamp(b,Slash,p+axis*2,new Vector2(radius*.96f,radius*1.37f),rotation,Glow(Lilac,fade*.55f));return;
        }
        if(mode==3||mode==4)
        {
            float size=radius*(mode==4?.45f:.65f)*(1+expansion*.6f);
            Flame(b,at-new Vector2(0,progress*12),new Vector2(.25f,-1),size,progress,mode,fade*(mode==4?.55f:1));
            return;
        }
        float spread=radius*(.18f+expansion*.82f);
        Stamp(b,Rupture,at,new Vector2(spread*2,spread*1.5f),progress*.12f,Glow(Violet,fade*.72f),Frame(Rupture,256,(int)(progress*8)));
        Stamp(b,Bloom,at,new Vector2(radius*(.65f+expansion*.2f)),0,Glow(Lilac,Math.Max(0,1-progress*7)*.8f));
        for(int i=0;i<(mode==1?8:5);i++)
        {
            float a=i*2.399963f+.42f;Vector2 point=at+new Vector2(MathF.Cos(a),MathF.Sin(a)*.6f)*spread*.4f-new Vector2(0,progress*radius*.2f);
            var frame=Frame(Veil,160,i+(int)(progress*5));
            Stamp(b,Veil,point,new Vector2(radius*.75f*(.7f+progress),radius*.55f),a*.2f,new Color(37,23,51,185)*(fade*.4f),frame);
            Flame(b,point,new Vector2(MathF.Cos(a)*.35f,-1),radius*(.25f+(i%3)*.06f),progress,i,fade*.62f);
        }
        if(mode is 1 or 5)Crown(b,at-new Vector2(0,spread*.27f),Math.Min(70,radius*.37f),progress,fade,6);
    }
    internal static void Ritual(SpriteBatch b,Vector2 at,float radius,float time,float opacity,int mode)
    {
        Ground(b,at,radius,time,opacity*.8f);
        int wisps=mode==3?7:mode==2?3:4;
        for(int i=0;i<wisps;i++)
        {
            float phase=(time*.8f+i/(float)wisps)%1,side=(i%2==0?1:-1);
            Vector2 point=at+new Vector2(side*radius*(.25f+.3f*MathF.Sin(phase*4+i)), -phase*(mode==3?100:55));
            Flame(b,point,new Vector2(side*.18f,-1),8+(1-phase)*(mode==3?26:16),time,i,opacity*MathF.Sin(phase*MathHelper.Pi)*.7f);
        }
        if(mode==3)Crown(b,at-new Vector2(0,50),50,time,opacity,6);
    }
    internal static void Ember(SpriteBatch b,Vector2 at,Vector2 velocity,float size,float progress,float phase,Color color,int kind,bool additive)
    {
        progress=Math.Clamp(progress,0,1);float fade=(1-progress)*(1-progress),rotation=Angle(velocity),opacity=.85f*fade;
        Color Tint(float strength) => additive
            ? new Color(color.R,color.G,color.B,(byte)(255*Math.Clamp(strength,0,1))) : Glow(color,strength);
        Color tint=Tint(opacity);
        if(kind==0)
            Stamp(b,Shard,at,new Vector2(size*(1.2f-progress*.5f),size*.18f),rotation,tint);
        else
            Stamp(b,Head,at,new Vector2(size*1.5f,size*.65f)*(1-progress*.4f),rotation,tint,Frame(Head,128,(int)(phase+progress*4)));
        if(kind!=0)Stamp(b,Bloom,at,new Vector2(size),0,Tint(opacity*.18f));
    }
    internal static void Smoke(SpriteBatch b,Vector2 at,float size,float progress,int frame,float rotation)
    {
        float fade=MathF.Sin(Math.Clamp(progress,0,1)*MathHelper.Pi);
        Stamp(b,Veil,at,new Vector2(size*(.8f+progress*.5f)),rotation,new Color(47,34,63,180)*(fade*.4f),Frame(Veil,160,frame));
    }
}
