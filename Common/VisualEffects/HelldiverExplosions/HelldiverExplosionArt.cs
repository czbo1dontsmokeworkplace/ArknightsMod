using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace ArknightsMod.Common.VisualEffects.HelldiverExplosions;

// Geometry, material layers and timing transplanted from HelldiversNeverDie.
// Theme colors are parameters; the effect has no gameplay or damage responsibilities.
internal enum ExplosionStyle { Plasma, Nuclear }
internal static class HelldiverExplosionArt
{
    internal const string Assets = "ArknightsMod/Content/Textures/HelldiverExplosions/";
    public static Texture2D Bloom,Streak,Smoke,Shock,Chip,Ion,Wake,Tufts;
    public static void Assign(Func<string,Texture2D> read)
    { Bloom=read("BloomMote");Streak=read("SparkStreak");Smoke=read("SmokePuffs");Shock=read("ShockBloom");Chip=read("Shrapnel");Ion=read("IonCore");Wake=read("IonWake");Tufts=read("FlameTufts"); }
    public static Color Add(Color c,float alpha=1)=>new(new Vector4(c.ToVector3()*Math.Max(0,alpha),0));
    public static Rectangle Frame(Texture2D t,int variant)=>new(0,Math.Abs(variant%4)*t.Width,t.Width,t.Width);
    public static void Glow(SpriteBatch b,Vector2 p,float size,Color c,float strength=1)
    {b.Draw(Bloom,p,null,Add(c,strength),0,Bloom.Size()/2,size/Bloom.Width,0,0);}
    public static void SmokePuff(SpriteBatch b,Vector2 p,float size,float age,Color tint,int variant,float spin=0,float opacity=.55f)
    {
        var f=Frame(Smoke,variant);float fade=MathF.Sin(Math.Clamp(age,0,1)*MathHelper.Pi);
        b.Draw(Smoke,p,f,tint*(fade*opacity),variant+spin,new Vector2(f.Width/2f),size/f.Width,0,0);
    }
    public static void Plasma(SpriteBatch b,Vector2 p,Vector2 velocity,int age,Color tint,float size,int seed,bool glow=true)
    {
        float rotation=velocity.ToRotation();Rectangle frame=new(0,((age/3+Math.Abs(seed))%4)*64,128,64);Vector2 origin=new(104,32);
        float pulse=1+.045f*MathF.Sin(age*.8f+seed);Vector2 scale=new Vector2(1.45f,.9f)*size/128*pulse;
        // Colored shoulder stays visible outside the small white-hot spear tip.
        b.Draw(Ion,p,frame,Add(tint,.3f),rotation,origin,scale*1.45f,0,0);
        b.Draw(Ion,p,frame,Add(tint,.92f),rotation,origin,scale,0,0);
        b.Draw(Ion,p,frame,Add(Color.Lerp(tint,Color.White,.85f),.9f),rotation,origin,scale*new Vector2(.75f,.37f),0,0);
        if(glow){Glow(b,p,size*1.3f,tint,.48f);Glow(b,p,size*.22f,Color.White,.85f);}
        b.Draw(Wake,p-velocity.SafeNormalize(Vector2.UnitX)*size*.35f,null,Add(tint,.45f),rotation, new Vector2(Wake.Width,Wake.Height/2),new Vector2(size*2/Wake.Width,size*.32f/Wake.Height),0,0);
    }
    public static void Flame(SpriteBatch b,Vector2 p,float rotation,float maturity,float size,int seed,Color theme,bool glow=true,float opacity=1)
    {
        maturity=Math.Clamp(maturity,0,1);float fade=MathF.Pow(1-maturity,.72f)*opacity;int frame=Math.Abs(seed+(int)(maturity*9))%4;
        Rectangle source=new(0,frame*128,128,128);Vector2 origin=new(64);float wobble=MathF.Sin(seed*1.7f+maturity*8)*.08f;
        Vector2 scale=new Vector2(1.25f,.92f)*size/128;
        var flip=(seed&1)==0?SpriteEffects.None:SpriteEffects.FlipVertically;
        // The soot-colored edge gives each packet a silhouette on bright terrain.
        b.Draw(Tufts,p,source,Add(theme*.35f)*(.42f*fade),rotation+wobble,origin,scale*1.14f,flip,0);
        Color shell=Color.Lerp(theme,theme*.65f,maturity);
        b.Draw(Tufts,p,source,Add(shell,fade*.86f),rotation+wobble,origin,scale,flip,0);
        if(maturity<.7f)b.Draw(Tufts,p,source,Add(Color.Lerp(theme,Color.White,.60f),fade*.72f),rotation-wobble,origin,scale*new Vector2(.76f,.43f),flip,0);
        if(maturity<.18f)b.Draw(Tufts,p,source,Add(Color.Lerp(theme,Color.White,.95f),fade*.55f),rotation,origin,scale*new Vector2(.52f,.18f),flip,0);
        if(glow)Glow(b,p,size,shell,fade*.13f);
    }
    public static void Explosion(SpriteBatch b,Vector2 p,float radius,float age,ExplosionStyle style,int seed,Color theme)
    {
        if(age<0||age>1)return;float fade=1-age;Color c=theme;
        if(radius<40){Glow(b,p,radius*1.7f,c,fade*.9f);Glow(b,p,radius*.55f,Color.White,fade);return;}

        var rng=new Random(seed);float expansion=1-MathF.Pow(1-age,3),flash=Math.Max(0,1-age*7);
        float shockFade=MathF.Pow(fade,2);b.Draw(Shock,p,null,Add(c,shockFade*.7f),seed,Shock.Size()/2,radius*(.55f+expansion*1.65f)/Shock.Width,0,0);
        Glow(b,p,radius*(.8f+age*.35f),c,.8f*fade*fade);Glow(b,p,radius*.9f,Color.White,flash*.92f);
        int lobes=style==ExplosionStyle.Nuclear?10:7;
        for(int i=0;i<lobes;i++){
            float angle=(float)rng.NextDouble()*MathHelper.TwoPi;Vector2 dir=angle.ToRotationVector2();float random=.25f+(float)rng.NextDouble()*.65f;
            Vector2 q=p+dir*radius*expansion*random*.38f;float size=radius*(.65f+(float)rng.NextDouble()*.55f)*fade;
            if(style == ExplosionStyle.Plasma)Plasma(b,q,dir,(int)(age*35)+i,c,size,seed+i);
            else Flame(b,q,(float)rng.NextDouble()*MathHelper.TwoPi,Math.Min(.98f,.18f+age*.8f),size,seed+i,c);
        }
        if(age>.12f){float smokeAge=(age-.12f)/.88f;for(int i=0;i<6;i++){float a=(float)rng.NextDouble()*MathHelper.TwoPi;Vector2 q=p+a.ToRotationVector2()*radius*expansion*.45f-new Vector2(0,age*radius*.25f);SmokePuff(b,q,radius*(.5f+age*.7f),smokeAge,new Color(38,37,40),seed+i,age*.2f,.42f);}}
        if(style==ExplosionStyle.Nuclear&&age>.2f){for(int i=0;i<5;i++){Vector2 q=p+new Vector2((i-2)*radius*.2f,-radius*(age-.2f)*1.2f);SmokePuff(b,q,radius*.8f,(age-.2f)/.8f,new Color(49,46,44),seed+i,age*.2f,.55f);Glow(b,q,radius*.5f,c,fade*.12f);}}
    }
}
