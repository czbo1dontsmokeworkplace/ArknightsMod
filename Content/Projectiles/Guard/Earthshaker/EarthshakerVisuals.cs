using System;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;
internal static class EarthshakerVisuals
{
    internal static bool Visible(Vector2 point)=>!Main.dedServ&&Vector2.DistanceSquared(point,Main.screenPosition+new Vector2(Main.screenWidth,Main.screenHeight)*.5f)<2200*2200;
    // Five original embers become 6/7 on alternating flames: mean 6.5, or +30%.
    internal static int FlameMoteCount(int identity)=>6+(identity&1);
    internal static void FlameBurst(Projectile flame)
    {
        if(!Visible(flame.Center))return;
        int count=FlameMoteCount(flame.identity);
        Vector2 axis=flame.velocity.SafeNormalize(-Vector2.UnitY);
        for(int i=0;i<count;i++){
            Vector2 point=flame.Center+Main.rand.NextVector2Circular(12,12);
            if(i>=5)point+=axis*Main.rand.NextFloat(20,65)*flame.ai[1];
            Dust.NewDustPerfect(point,DustID.Torch,flame.velocity*Main.rand.NextFloat(.05f,.18f),0,default,1.1f);
        }
    }
    internal static void Raise(Vector2 p,EarthshakerKind kind){if(Visible(p))SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with{Volume=.5f,Pitch=-.55f,MaxInstances=3},p);}
    internal static void Impact(Vector2 foot,EarthshakerKind kind,float force,float width,bool sound,float gravity)
    {
        if(!Visible(foot))return;
        // Flint's compact ground plume, native tile chips, smoke and golden sparks.
        for(int i=0;i<30;i++){
            Vector2 v=Main.rand.NextVector2Circular(5,3);v.Y=-Math.Abs(v.Y)*gravity;
            Dust.NewDustPerfect(foot+Main.rand.NextVector2Circular(7,0),DustID.Torch,v,0,default,Main.rand.NextFloat(.7f,1.5f));
        }
        Point origin=foot.ToTileCoordinates();int sign=gravity<0?-1:1;
        for(int x=origin.X-3;x<=origin.X+3;x++){
            int y=origin.Y;if(!EarthshakerFlameLine.FindSurface(x,ref y,gravity))continue;
            if(EarthshakerFlameLine.Walkable(x,y-sign))continue;
            Vector2 at=new(x*16+8,y*16+(sign<0?16:0));
            for(int i=0;i<6;i++)Dust.NewDustPerfect(at+Main.rand.NextVector2Circular(7,2),DustID.Stone,new Vector2(Main.rand.NextFloat(-2,2),-Main.rand.NextFloat(1,7)*gravity),0,default,Main.rand.NextFloat(.8f,1.4f));
            if(x%2==0){var gore=Gore.NewGoreDirect(new Terraria.DataStructures.EntitySource_Misc("FlintImpact"),at,new Vector2(Main.rand.NextFloat(-1,1),-Main.rand.NextFloat(.5f,2.5f)*gravity),61+Main.rand.Next(3),.65f);
                gore.velocity.Y=-Math.Abs(gore.velocity.Y)*gravity;}
            ChargedOperatorVfx.Sparkle(at,new Vector2(0,-2*gravity),new Color(255,210,100),MathHelper.PiOver2,.65f);
        }
        if(sound)SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with{Volume=.9f,Pitch=-.35f},foot);
    }
}
