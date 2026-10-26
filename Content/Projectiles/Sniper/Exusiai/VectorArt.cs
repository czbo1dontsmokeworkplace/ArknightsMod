using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

// Shared by production and GPU review. Every shape is a material, not a drawn line.
public static class VectorArt
{
    private static Texture2D muzzle, bullet, streak, flight, feather, bloom, ring, pressure, smoke;
    public static void Assign(Func<string, Texture2D> load)
    {
        muzzle=load("Vector/MuzzleFlash"); bullet=load("Vector/PrecisionRound"); streak=load("Vector/SparkStreak");
        flight=load("Vector/FlightWisp"); feather=load("Vector/Feather"); pressure=load("Vector/PressureFlash");
        bloom=load("ShpcEnergy/BloomCircle"); ring=load("ShpcEnergy/BloomRing"); smoke=load("HelldiverExplosions/SmokePuffs");
    }
    public static void Clear() => muzzle=bullet=streak=flight=feather=bloom=ring=pressure=smoke=null;
    public static Color Theme(int mode) => mode==3?new(255,126,64):mode==2?new(255,192,86):new(255,211,123);
    public static Color RoundColor => new(255,174,58); // Exusiai's orange-gold palette, retaining R-2's material.
    private static Color Add(Color color,float opacity) => new(new Vector4(color.ToVector3()*Math.Clamp(opacity,0,1),0));
    private static Vector2 Size(Texture2D t) => new(t.Width,t.Height);
    private static void Glow(SpriteBatch batch,Vector2 at,Vector2 size,Color color,float opacity)
        => batch.Draw(bloom,at,null,Add(color,opacity),0,Size(bloom)*.5f,size/Size(bloom),SpriteEffects.None,0);

    public static void Round(SpriteBatch batch,Vector2 head,Vector2 direction,float travelled,int mode,float opacity=1)
    {
        if(travelled<1)return;
        // Whole R-2 material, orange shell and shorter pale core. No stretched cropped sliver.
        float rotation=MathF.Atan2(direction.Y,direction.X)+MathF.PI/2;
        Vector2 origin=Size(bullet)*.5f;
        batch.Draw(bullet,head,null,new Color(48,26,12)*(.9f*opacity),rotation,origin,new Vector2(1.6f,.78f),0,0);
        batch.Draw(bullet,head,null,Add(RoundColor,opacity),rotation,origin,new Vector2(.95f,.8f),0,0);
        batch.Draw(bullet,head,null,Add(Color.Lerp(RoundColor,Color.White,.7f),opacity),rotation,origin,new Vector2(.27f,.52f),0,0);
    }
    public static void Muzzle(SpriteBatch batch,Vector2 point,float angle,float flash,int mode)
    {
        if(flash<=.01f)return;
        batch.Draw(muzzle,point,null,Add(Theme(mode),.65f*flash),angle,new Vector2(0,muzzle.Height/2f),
            new Vector2(43f/muzzle.Width,32f/muzzle.Height),0,0);
        batch.Draw(muzzle,point,null,Add(Color.White,.72f*flash),angle,new Vector2(0,muzzle.Height/2f),
            new Vector2(23f/muzzle.Width,14f/muzzle.Height),0,0);
    }
    public static Vector2 WingRoot(Vector2 center,int facing,int side,float t,VectorWingPose pose)
    {
        var p=Piece(pose,t);if(p==null)return center;
        return center+pose.World(side>0?p.Front:p.Back)-pose.World(pose.Center);
    }
    private static VectorWingFeather Piece(VectorWingPose pose,float t)
    {
        if(pose.Pieces==null)return null;int index=(int)MathF.Round(Math.Clamp(t,0,1)*6);
        foreach(var p in pose.Pieces)if(p.Active&&p.Index==index)return p;
        return null;
    }
    public static Vector2 WingTip(Vector2 center,int facing,int side,float t,float time,VectorWingPose pose)
    {
        var p=Piece(pose,t);if(p==null)return center;
        return WingTip(center,side,p,pose);
    }
    public static Vector2 WingTip(Vector2 center,int side,VectorWingFeather p,VectorWingPose pose)
    {
        Vector2 root=center+pose.World(side>0?p.Front:p.Back)-pose.World(pose.Center);
        float angle=(side>0?p.FrontRotation:p.BackRotation)+MathF.PI/2;
        Vector2 direction=new(MathF.Cos(angle),MathF.Sin(angle)*pose.Gravity);
        return root+direction*PieceLength(p,side>0);
    }
    private static float PieceLength(VectorWingFeather p,bool front) => front ? p.FrontLength : p.BackLength;
    private delegate void WingDraw(Texture2D texture,Vector2 at,Rectangle? source,Color color,float rotation,Vector2 origin,Vector2 scale);
    private static void WingGlow(WingDraw draw,Vector2 at,Vector2 size,Color color,float opacity)
        => draw(bloom,at,null,Add(color,opacity),0,Size(bloom)*.5f,size/Size(bloom));
    private static void Plume(WingDraw draw,Vector2 root,Vector2 tip,float width,Color color,float opacity)
    {
        // First atlas frame is a filled feather pointing bottom-left to top-right.
        var source=new Rectangle(0,0,feather.Width,feather.Height/3);
        Vector2 delta=tip-root;
        Vector2 scale=new(width/(source.Width*.72f),delta.Length()/(source.Height-4));
        // Nonuniform stretch changes the source diagonal's angle. Correct AFTER scaling,
        // so mirrored wings and shedding points share their actual intended tips.
        float sourceAngle=MathF.Atan2(-(source.Height-4)*scale.Y,(source.Width-4)*scale.X);
        float angle=MathF.Atan2(delta.Y,delta.X)-sourceAngle;
        Vector2 origin=new(2,source.Height-2);
        draw(feather,root,source,new Color(91,32,23)*(.4f*opacity),angle,origin,scale*1.05f);
        draw(feather,root,source,color*opacity,angle,origin,scale);
        draw(feather,root,source,Add(Color.Lerp(color,Color.White,.7f),opacity*.48f),angle,origin,scale*new Vector2(.66f,.93f));
    }
    public static void Aura(SpriteBatch batch,Vector2 center,int facing,int mode,float time,float opacity)
        => Aura(batch,center,facing,mode,time,opacity,default);
    public static void Aura(SpriteBatch batch,Vector2 center,int facing,int mode,float time,float opacity,VectorWingPose pose)
        => DrawWings((texture,at,source,color,rotation,origin,scale)=>
            batch.Draw(texture,at,source,color,rotation,origin,scale,SpriteEffects.None,0),center,facing,mode,opacity,pose);
    // Player layers must append to the cache; drawing directly here bypasses the player's layering.
    // Both the live player and GPU review use the exact same material/geometry path.
    public static void CollectWings(List<DrawData> cache,Vector2 center,int facing,VectorWingPose pose)
        => DrawWings((texture,at,source,color,rotation,origin,scale)=>
            cache.Add(new DrawData(texture,at,source,color,rotation,origin,scale,SpriteEffects.None)),center,facing,3,1,pose);
    private static void DrawWings(WingDraw draw,Vector2 center,int facing,int mode,float opacity,VectorWingPose pose)
    {
        if(mode<1||opacity<=.001f||pose.Pieces==null||pose.Presence<=.001f)return;
        Color color=Theme(mode);
        {
            // Two coherent shoulder-anchored fans; each feather is part of the same wing.
            for(int pass=0;pass<2;pass++)foreach(var p in pose.Pieces)if(p.Active&&p.FadePower>.0001f) {
                bool front=pass==1;int side=front?1:-1;
                Vector2 root=center+pose.World(front?p.Front:p.Back)-pose.World(pose.Center);
                float angle=(front?p.FrontRotation:p.BackRotation)+MathF.PI/2;
                Vector2 direction=new(MathF.Cos(angle),MathF.Sin(angle)*pose.Gravity);
                Vector2 tip=root+direction*PieceLength(p,front);
                float width=p.Width*(front?1:.88f);
                float alpha=p.FadePower*opacity;
                Color body=Color.Lerp(new Color(255,178,70),new Color(255,235,184),p.Index/6f*.65f);
                Plume(draw,root,tip,width,body,alpha);
                WingGlow(draw,root,new Vector2(14,9),color,alpha*.16f);
                if(p.MotionGlow>.1f)WingGlow(draw,root,new Vector2(14,18),
                    Color.Lerp(color,Color.White,.65f),alpha*.45f*MathF.Pow(p.MotionGlow,3));
            }
        }
        float presence=pose.Presence;
        Vector2 halo=center+new Vector2(-facing*2,-40);
        draw(ring,halo,null,Add(new Color(255,205,110),opacity*presence*.72f),-.12f,
            Size(ring)*.5f,new Vector2(33,11)/Size(ring));
    }
    public static void Impact(SpriteBatch batch,Vector2 point,Vector2 reverse,float age,int seed,int mode)
    {
        // Short R-2 contact flash; independently moving sparks provide the aftermath.
        float flash=Math.Clamp(1-age*15/4,0,1);
        if(flash<=0)return;
        var source=new Rectangle(0,256,192,128);
        float rotation=MathF.Atan2(reverse.Y,reverse.X);
        Vector2 scale=new Vector2(48/192f,48/192f*.8f);
        Color color=new(255,195,103);
        batch.Draw(pressure,point,source,Add(color,.9f*flash),rotation,new Vector2(12,64),scale,0,0);
        batch.Draw(pressure,point,source,Add(Color.Lerp(color,Color.White,.85f),flash),rotation,new Vector2(12,64),scale*new Vector2(.7f,.38f),0,0);
    }
    public static void Material(SpriteBatch batch,Vector2 at,Vector2 velocity,float age,int variant,float size,Color color,float rotation,float opacity)
    {
        age=Math.Clamp(age,0,1);float fade=(1-age*age*age)*opacity;
        if(variant==0) {
            Vector2 scale=new Vector2(1.6f,.28f)*size/flight.Width*MathF.Sqrt(1-age);
            batch.Draw(flight,at,null,new Color(38,20,8)*(.5f*fade),rotation,Size(flight)*.5f,scale*new Vector2(1,1.5f),0,0);
            batch.Draw(flight,at,null,Add(color,fade*.55f),rotation,Size(flight)*.5f,scale,0,0);
        }
        else if(variant==1) {
            Glow(batch,at,new Vector2(size*1.3f),color,fade*.12f);
            Vector2 scale=new Vector2(1.8f,.32f)*size/streak.Width*MathF.Sqrt(1-age);
            batch.Draw(streak,at,null,Add(color,fade),rotation,Size(streak)*.5f,scale,0,0);
            batch.Draw(streak,at,null,Add(Color.Lerp(color,Color.White,.8f),fade*.8f),rotation,Size(streak)*.5f,scale*new Vector2(.9f,.36f),0,0);
        }
        else if(variant==2) {
            var source=new Rectangle(0,0,feather.Width,feather.Height/3);
            float envelope=Math.Min(1,age*7)*MathF.Pow(1-age,1.2f);
            batch.Draw(feather,at,source,color*(envelope*opacity),rotation,new Vector2(source.Width,source.Height)*.5f,size/source.Width,0,0);
            Glow(batch,at,new Vector2(size*1.7f),color,envelope*opacity*.1f);
        }
        else if(variant==3) {
            Glow(batch,at,new Vector2(size*2),color,fade*.28f);
            Glow(batch,at,new Vector2(size*.65f),Color.Lerp(color,Color.White,.7f),fade);
        }
        else {
            var source=new Rectangle(0,((int)rotation%4+4)%4*smoke.Width,smoke.Width,smoke.Width);
            batch.Draw(smoke,at,source,new Color(94,90,85)*(MathF.Sin(age*MathF.PI)*opacity*.3f),rotation,
                new Vector2(source.Width*.5f),size*(1+age*.6f)/source.Width,0,0);
        }
    }
}
