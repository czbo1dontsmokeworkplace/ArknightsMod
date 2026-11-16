using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// EssenceofSnow_N2's visual emission is used verbatim: same 50-tick expansion,
// 10 medium mists, six-tick 18-mist pulse, and eight heavy smokes per update.
// The two particle implementations/textures below are ported from local Calamity.
public sealed class FrostNovaColdMistSystem : ModSystem
{
    private sealed class Emitter { internal Vector2 Center,Velocity; internal int Age; }
    private static readonly List<Emitter> emitters=new();
    internal static int EmitterCount=>emitters.Count;
    internal static bool Room => ParticleManager.activeParticles.Count<1800;
    internal static void Begin(Vector2 center,Vector2 velocity=default)
    {
        if(Main.dedServ||!FrostNovaVisuals.Visible(center,800))return;
        foreach(var emitter in emitters)if(emitter.Age<12&&Vector2.DistanceSquared(emitter.Center,center)<100*100)return;
        if(emitters.Count>=2)return;
        emitters.Add(new Emitter{Center=center,Velocity=velocity});
    }
    internal static void EmitSnowFrame(Vector2 center,int timer)
    {
        if(Main.dedServ)return;
        float progress=Utils.GetLerpValue(0f,50f,timer,true);
        float sizeFactor=MathHelper.Lerp(1f,2f,progress);
        float radius=(int)(280*sizeFactor)*.5f;
        for(int i=0;i<10&&Room;i++)
            new FrostNovaMediumMist(center+Main.rand.NextVector2Circular(radius,radius),
                new Vector2(Main.rand.NextFloat(-1.2f,1.2f),Main.rand.NextFloat(-6f,-2f)),Color.White,Color.Transparent,
                Main.rand.NextFloat(.6f,1.1f),Main.rand.NextFloat(200f,300f)).Spawn();
        if(timer%6==0)for(int i=0;i<18&&Room;i++)
        {
            float angle=MathHelper.TwoPi/18*i+timer*.03f;
            Vector2 pos=center+angle.ToRotationVector2()*60;
            new FrostNovaMediumMist(pos,(pos-center).SafeNormalize(Vector2.Zero)*2,Color.White,Color.Transparent,.7f,180).Spawn();
        }
        for(int i=0;i<8&&Room;i++)
            new FrostNovaHeavySmoke(center+Main.rand.NextVector2Circular(radius*.6f,radius*.6f),
                new Vector2(Main.rand.NextFloat(-1,1),Main.rand.NextFloat(-3.5f,-1.2f)),
                Color.Lerp(Color.White,Color.WhiteSmoke,.3f),22,Main.rand.NextFloat(1.2f,1.8f),.55f,Main.rand.NextFloat(-0f,0f),true).Spawn();
    }
    internal static void Surface(Vector2 center)
    {
        if(Main.dedServ||!Room)return;
        new FrostNovaHeavySmoke(center,new Vector2(Main.rand.NextFloat(-.6f,.6f),Main.rand.NextFloat(-1.4f,-.5f)),
            new Color(155,195,215),22,Main.rand.NextFloat(.28f,.46f),.32f,0,true).Spawn();
    }
    public override void PostUpdateEverything()
    {
        if(Main.dedServ||Main.gamePaused)return;
        foreach(var emitter in emitters){emitter.Age++;emitter.Velocity*=.8f;emitter.Center+=emitter.Velocity;EmitSnowFrame(emitter.Center,emitter.Age);}
        emitters.RemoveAll(e=>e.Age>=50);
    }
    public override void OnWorldUnload()=>emitters.Clear();
    public override void Unload()=>emitters.Clear();
}

public sealed class FrostNovaMediumMist : Particle
{
    public override string TexturePath=>FrostNovaVisuals.Assets+"MediumMist";
    public override int FrameCount=>3;
    public override BlendState DrawBlendState=>BlendState.Additive;
    private float mistOpacity,spin;private Color fire,fade;
    public FrostNovaMediumMist(Vector2 position,Vector2 velocity,Color colorFire,Color colorFade,float scale,float opacity,float rotationSpeed=0)
    {
        Position=position;Velocity=velocity;fire=colorFire;fade=colorFade;Scale=scale;mistOpacity=opacity;
        Rotation=Main.rand.NextFloat(MathHelper.TwoPi);spin=rotationSpeed;Frame=Main.rand.Next(3);Lifetime=600;Direction=1;
    }
    public override void Update()
    {
        Vector2 previousVelocity=Velocity;
        Rotation+=spin*(Velocity.X>0?1:-1);Velocity*=.85f;
        if(mistOpacity>90){if(!Main.dedServ)Lighting.AddLight(Position,Color.ToVector3()*.1f);Scale+=.01f;mistOpacity-=3;}
        else{Scale*=.975f;mistOpacity-=2;}
        if(mistOpacity<0)Kill();
        Color=Color.Lerp(fire,fade,MathHelper.Clamp(((255-mistOpacity)-100)/80,0,1))*(mistOpacity/255);
        // Calamity moves before Update; this project's manager moves after it.
        Position+=previousVelocity-Velocity;
    }
}

public sealed class FrostNovaHeavySmoke : Particle
{
    public override string TexturePath=>FrostNovaVisuals.Assets+"HeavySmoke";
    public override BlendState DrawBlendState=>glowing?BlendState.Additive:BlendState.NonPremultiplied;
    private float smokeOpacity,spin,hueShift;private bool glowing;private int variant;
    public FrostNovaHeavySmoke(Vector2 position,Vector2 velocity,Color color,int lifetime,float scale,float opacity,
        float rotationSpeed=0,bool glowing=false,float hueshift=0)
    {
        Position=position;Velocity=velocity;Color=color;Scale=scale;variant=Main.rand.Next(7);Lifetime=lifetime;
        smokeOpacity=opacity;spin=rotationSpeed;this.glowing=glowing;hueShift=hueshift;Direction=1;
    }
    public override void Update()
    {
        Vector2 previousVelocity=Velocity;
        if(Time/(float)Lifetime<.2f)Scale+=.01f;else Scale*=.975f;
        Color=Main.hslToRgb((Main.rgbToHsl(Color).X+hueShift)%1,Main.rgbToHsl(Color).Y,Main.rgbToHsl(Color).Z);
        smokeOpacity*=.98f;Rotation+=spin*(Velocity.X>0?1:-1);Velocity*=.85f;
        Color*=Utils.GetLerpValue(1,.85f,LifetimeRatio,true);Position+=previousVelocity-Velocity;
    }
    public override void Draw()
    {
        if(Time>=Lifetime)return;
        int animation=(int)Math.Floor(Time/(Lifetime/6f));Rectangle frame=new(80*variant,80*animation,80,80);
        Main.spriteBatch.Draw(Texture,Position-Main.screenPosition,frame,Color*smokeOpacity,Rotation,frame.Size()/2,Scale,SpriteEffects.None,0);
    }
}

public sealed class FrostNovaGoldWake : Particle
{
    public override string TexturePath=>"Terraria/Images/Extra_"+ExtrasID.ThePerfectGlow;
    public override BlendState DrawBlendState=>BlendState.Additive;
    public FrostNovaGoldWake(Vector2 at,Vector2 velocity)
    {Position=at;Velocity=velocity;Lifetime=15;Scale=1;Direction=1;Color=new Color(255,210,48);}
    public override void Update(){Velocity*=.9f;Opacity=MathF.Pow(1-LifetimeRatio,1.6f);}
    public override void Draw()
    {
        float size=9*(1-LifetimeRatio)+2;
        Main.spriteBatch.Draw(Texture,Position-Main.screenPosition,null,Color*Opacity,0,Texture.Size()/2,new Vector2(size)/Texture.Size(),SpriteEffects.None,0);
    }
}

public partial class FrostNova
{
    private int coldClock;
    private void UpdateColdAir()
    {
        if(Main.dedServ)return;
        coldClock++;
        if(CombatHazardsActive&&coldClock%150==1)FrostNovaColdMistSystem.Begin(NPC.Center,new Vector2(0,-.25f));
        if(AirbornePhase)for(int i=0;i<FrostNovaHaloRules.Curves;i++)
        {
            Vector2 now=FrostNovaHaloRules.Orbit(i,Main.GlobalTimeWrappedHourly,0);
            Vector2 previous=FrostNovaHaloRules.Orbit(i,Main.GlobalTimeWrappedHourly-1/60f,0);
            if(FrostNovaColdMistSystem.Room)new FrostNovaGoldWake(NPC.Center+now,(now-previous)*.13f).Spawn();
        }
    }
}
