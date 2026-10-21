using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

internal static class ReaperVisuals
{
    private static readonly Dictionary<ReaperKind, Texture2D> art = new();
    internal static void Unload() => art.Clear();
    internal static bool Visible(Vector2 point) => !Main.dedServ && Vector2.DistanceSquared(point,Main.screenPosition+new Vector2(Main.screenWidth,Main.screenHeight)*.5f)<1600*1600;
    internal static void Blade(Vector2 grip,Vector2 tip,ReaperKind kind,float opacity)
    {
        if(!Visible(grip)||opacity<=0)return;
        if(!art.TryGetValue(kind,out Texture2D texture)) art[kind]=texture=ModContent.Request<Texture2D>(ReaperRules.Art(kind), AssetRequestMode.ImmediateLoad).Value;
        // Scythes have a long lower-left handle and upper-right blade. Entelechia's
        // supplied image uses this same source axis and retains its curved head.
        Vector2 origin=ReaperRules.Grip(kind);
        Vector2 head=ReaperRules.Head(kind);
        float scale=Vector2.Distance(grip,tip)/Vector2.Distance(origin,head);
        float rotation=(tip-grip).ToRotation()-(head-origin).ToRotation();
        Main.EntitySpriteDraw(texture,grip-Main.screenPosition,null,Color.White*opacity,rotation,origin,scale,SpriteEffects.None);
        using(new ChargedOperatorVfx.Pass(true)) {
            ChargedOperatorVfx.Sprite("OrbitSmear",tip,new Vector2(39,20)*ReaperRules.LeftScale,ReaperRules.Color(kind)*(.65f*opacity),rotation);
            ChargedOperatorVfx.Sprite("StarCore",tip,new Vector2(14)*ReaperRules.LeftScale,ReaperRules.Edge(kind)*(.85f*opacity));
        }
    }
    internal static void Shed(Vector2 point,Vector2 velocity,ReaperKind kind)
    {
        if(!Visible(point)||ParticleManager.activeParticles.Count>1100)return;
        new T_Reaper_Petal(point,velocity*.1f+Main.rand.NextVector2Circular(.8f,.8f),kind,18,7).Spawn();
    }
    internal static void CutMotes(Vector2 grip,Vector2 tip,Vector2 tangent,ReaperKind kind)
    {
        if(!Visible(tip))return;
        for(int i=0;i<3&&ParticleManager.activeParticles.Count<1400;i++){
            Vector2 point=Vector2.Lerp(grip,tip,Main.rand.NextFloat(.55f,1));
            new T_Centurion_Blood_Line_Particle(point,tangent*Main.rand.NextFloat(5,14),Main.rand.Next(15,25),Main.rand.NextFloat(.5f,.9f),Color.Lerp(new Color(220,225,235),ReaperRules.Edge(kind),.2f)).Spawn();
            Shed(point,tangent*4,kind);
        }
        for(int i=0;i<4;i++){
            var d=Dust.NewDustPerfect(Vector2.Lerp(grip,tip,Main.rand.NextFloat(.5f,1)),Terraria.ID.DustID.SilverCoin,tangent*Main.rand.NextFloat(1,5),0,default,.8f);d.noGravity=true;
        }
    }
    internal static void DrawWave(Projectile q)
    {
        if(!Visible(q.Center))return;
        // Cherry's existing procedural crescent is the travelling projectile itself.
        ReaperMaterial.LegacyCrescent(q.Center,q.rotation,90f/.72f*q.scale,q.Opacity,1,true);
    }
    internal static void Hit(Vector2 point,ReaperKind kind,float strength)
    {
        if(!Visible(point))return;
        int count=Math.Clamp((int)(10*strength),3,16);
        for(int i=0;i<count;i++) {
            Vector2 ray=(i*MathHelper.TwoPi/count).ToRotationVector2();
            if(ParticleManager.activeParticles.Count<1200)new T_Reaper_Petal(point,ray*Main.rand.NextFloat(2,5),kind,18+i%10,8*strength).Spawn();
        }
        ChargedOperatorVfx.Sparkle(point,Vector2.Zero,ReaperRules.Edge(kind),.6f,.8f*strength);
    }
    internal static void Drain(Vector2 point,Vector2 owner,ReaperKind kind)
    {
        if(!Visible(point))return;
        Hit(point,kind,.8f);
        for(int i=0;i<3;i++)if(ParticleManager.activeParticles.Count<1200)
            new T_Reaper_Petal(point+Main.rand.NextVector2Circular(7,7),(owner-point).SafeNormalize(Vector2.Zero)*4,kind,23+i*2,6,owner).Spawn();
    }
    internal static void RightSwing(Player owner,ReaperKind kind) => Hit(owner.MountedCenter+new Vector2(owner.direction*30,-12),kind,.35f);
    internal static void Activate(Player owner,ReaperKind kind)
    {
        Hit(owner.Center,kind,1.3f);
        if(!Main.dedServ)SoundEngine.PlaySound(Terraria.ID.SoundID.Item29 with{Volume=.55f,Pitch=.15f},owner.Center);
    }
}
