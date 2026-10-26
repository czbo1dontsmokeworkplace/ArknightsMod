using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

internal static class CrusherAnchorVisuals
{
    private const CrusherKind Kind=CrusherKind.Ulpianus;
    internal static void Launch(Projectile q)
    {
        if(!CrusherVisuals.Visible(q.Center))return;
        SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with{Volume=.7f,Pitch=-.45f,MaxInstances=3},q.Center);
        Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX);
        for(int i=0;i<10;i++){
            var d=Dust.NewDustPerfect(q.Center+Main.rand.NextVector2Circular(10,10),DustID.Water,
                -axis*Main.rand.NextFloat(2,5)+Main.rand.NextVector2Circular(2,2),70,default,1.1f);d.noGravity=true;
        }
    }
    internal static void Flight(Projectile q)
    {
        if(!CrusherVisuals.Visible(q.Center))return;
        Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX);
        for(int i=0;i<2;i++){
            var d=Dust.NewDustPerfect(q.Center-axis*18+Main.rand.NextVector2Circular(9,9),DustID.Water,-axis*2,80,default,.95f);d.noGravity=true;
        }
        if(ParticleManager.activeParticles.Count<1100)new T_Crusher_Fragment(q.Center-axis*22,-axis*2,Kind,16,7,false).Spawn();
    }
    internal static void DrawFlight(Projectile q,Vector2[] history,int samples,bool trailingChain=true)
    {
        if(!CrusherVisuals.Visible(q.Center))return;
        CrusherWaveMaterial.DrawAnchorTrail(q,history,samples);
        // A short trailing chain follows flight history, rather than a screen-long tether.
        Texture2D chain=TextureAssets.Chain.Value;
        for(int i=2;trailingChain&&i<Math.Min(samples,12);i++){
            Vector2 at=history[i],delta=history[i-1]-at;
            if(delta.LengthSquared()<.01f||delta.LengthSquared()>60*60)continue;
            Main.EntitySpriteDraw(chain,at-Main.screenPosition,null,new Color(140,196,220)*(.6f*(1-i/12f)),
                delta.ToRotation()-MathHelper.PiOver2,chain.Size()*.5f,.8f,SpriteEffects.None);
        }
        Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX);
        float reach=CrusherRules.Reach(Kind)*1.1f;
        for(int i=Math.Min(samples-1,9);i>=3;i-=3){
            Vector2 at=history[i];float fade=.12f*(1-i/12f);
            CrusherVisuals.DrawBlade(at-axis*reach*.5f,at+axis*reach*.5f,Kind,fade,false);
        }
        CrusherVisuals.DrawBlade(q.Center-axis*reach*.5f,q.Center+axis*reach*.5f,Kind,1,false);
    }
    internal static void DrawThrowTether(Projectile q)
    {
        if (!CrusherVisuals.Visible(q.Center)) return;
        Texture2D chain = TextureAssets.Chain.Value;
        Vector2 a = Main.player[q.owner].MountedCenter;
        Vector2 z = q.Center - q.velocity.SafeNormalize(Vector2.UnitX) * CrusherRules.Reach(Kind) * .55f;
        float length = Vector2.Distance(a, z);
        if (!float.IsFinite(length) || length > 1400) return;
        int count = Math.Clamp((int)(length / Math.Max(8, chain.Height * .75f)), 1, 120);
        Vector2 Point(float t) => Vector2.Lerp(a, z, t) + Vector2.UnitY * MathF.Sin(t * MathHelper.Pi) * Math.Min(65, length * .08f);
        for (int i = 0; i < count; i++)
        {
            Vector2 at = Point(i / (float)count), next = Point((i + 1f) / count);
            Main.EntitySpriteDraw(chain, (at + next) / 2 - Main.screenPosition, null, new Color(116, 170, 201),
                (next - at).ToRotation() - MathHelper.PiOver2, chain.Size() / 2, .9f, SpriteEffects.None);
        }
    }
    internal static void Impact(Projectile q)
    {
        if(!CrusherVisuals.Visible(q.Center))return;
        float strength=Math.Clamp(q.ai[0],.35f,1.5f);Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX);
        foreach(var p in CrusherVisuals.BurstParticles(q.Center,axis,Kind,strength))if(ParticleManager.activeParticles.Count<1200)p.Spawn();
        for(int i=0;i<(int)(18*strength);i++){
            Vector2 direction=Main.rand.NextVector2CircularEdge(1,1);
            var d=Dust.NewDustPerfect(q.Center+direction*7,i%3==0?DustID.SilverCoin:DustID.Water,
                direction*Main.rand.NextFloat(3,7)*strength-axis*1.5f,60,default,Main.rand.NextFloat(.9f,1.3f));d.noGravity=true;
        }
        SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with{Volume=.65f*strength,Pitch=-.35f,MaxInstances=3},q.Center);
        if(q.owner==Main.myPlayer&&Vector2.DistanceSquared(Main.LocalPlayer.Center,q.Center)<850*850)
            Main.instance?.CameraModifiers?.Add(new PunchCameraModifier(q.Center,axis,3.2f*strength,6,8,850,"UlpianusAnchorHit"));
    }
    internal static void DrawImpact(Projectile q)
    {
        if(!CrusherVisuals.Visible(q.Center))return;
        float t=1-q.timeLeft/22f,strength=Math.Clamp(q.ai[0],.35f,1.5f),fade=(1-t)*(1-t);
        Vector2 axis=q.velocity.SafeNormalize(Vector2.UnitX);float rotation=axis.ToRotation();
        CrusherMaterial.DrawImpact(q.Center,axis,Kind,t,strength);
        using(new ChargedOperatorVfx.Pass(true)){
            ChargedOperatorVfx.Sprite("RuptureRing",q.Center,new Vector2(36+150*t,30+105*t)*strength,CrusherRules.Edge(Kind)*(fade*.8f),rotation);
            ChargedOperatorVfx.Sprite("ImpactPetals",q.Center,new Vector2(100,80)*strength,CrusherRules.Tint(Kind)*(fade*.85f),rotation);
            ChargedOperatorVfx.Sprite("LinearSpark",q.Center,new Vector2(110,28)*strength,CrusherRules.Edge(Kind)*(fade*.8f),rotation+MathHelper.PiOver2);
            ChargedOperatorVfx.Sprite("StarCore",q.Center,new Vector2(48)*strength,Color.White*(fade*.75f));
        }
    }
}

// Networked cosmetic event: no additional damage, pull or hit immunity changes.
public sealed class CrusherAnchorFeedback:ModProjectile
{
    public override string Texture=>"Terraria/Images/MagicPixel";
    public override void SetDefaults(){Projectile.width=Projectile.height=2;Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.timeLeft=22;Projectile.netImportant=true;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>false;
    public override void AI(){if(Projectile.localAI[0]++==0)CrusherAnchorVisuals.Impact(Projectile);}
    public override bool PreDraw(ref Color lightColor){CrusherAnchorVisuals.DrawImpact(Projectile);return false;}
}
