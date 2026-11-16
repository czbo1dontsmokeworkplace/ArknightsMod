using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using ReLogic.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// Authored black-ice sprites retain their original silhouette. Invisible attack
// coordinators never acquire decorative circles through this shared fade renderer.
internal static class FrostNovaProjectileArt
{
    internal static readonly Dictionary<string,Texture2D> ExternalTextures=new();
    internal static bool Supports(Projectile p) => p.ModProjectile is not (FrostNovaJump or FrostNovaRevival
        or FrostNovaBlackIceRegion or SnowSupportPortal);
    internal static Texture2D Native(int type)
    {
        if(!TextureAssets.Projectile[type].IsLoaded) Main.instance.LoadProjectile(type);
        return TextureAssets.Projectile[type].Value;
    }
    internal static void Crystal(Vector2 at,Vector2 size,float angle,float opacity,bool black=false)
    {
        Texture2D tex=black?FrostNovaVisuals.Texture("BlackIce"):Native(ProjectileID.IceBolt);
        Rectangle frame=tex.Frame(1,black?1:Math.Max(1,Main.projFrames[ProjectileID.IceBolt]),0,0);
        Main.spriteBatch.Draw(tex,at-Main.screenPosition,frame,Color.White*opacity,
            angle,frame.Size()/2,size/frame.Size(),SpriteEffects.None,0);
    }
    private static void Original(Projectile p,string name,int frames,float opacity,float scale=1)
    {
        Texture2D tex;
        if(name.Contains('/')){if(!ExternalTextures.TryGetValue(name,out tex))ExternalTextures[name]=tex=ModContent.Request<Texture2D>(name,AssetRequestMode.ImmediateLoad).Value;}
        else tex=FrostNovaVisuals.Texture(name);
        Rectangle frame=tex.Frame(1,frames,0,Math.Clamp(p.frame,0,frames-1));
        Vector2 size=new(p.scale*scale);
        FrostNovaVisuals.Outline(tex,p.Center,frame,p.rotation,frame.Size()/2,size,opacity*.65f,frames);
        Main.spriteBatch.Draw(tex,p.Center-Main.screenPosition,frame,Color.White*opacity,p.rotation,frame.Size()/2,size,SpriteEffects.None,0);
    }
    internal static void Draw(Projectile p,float visibility)
    {
        if(Main.dedServ||visibility<=.001f)return;
        if(p.ModProjectile is FrostNovaRitualPulse or FNIceRingChoice or FNIceRingRound
            or IceAltarTwinLineRing or FNInvincibleEffect or BlizzardStormStarter or FrostNovaWhiteRing or IceAltarShockwave)return;
        if(p.ModProjectile is Blizzard or FrostNovaSmoke or FNIceDust)return; // Actual particle emitters.
        if(p.ModProjectile is Oneirosbomb){Original(p,"ArknightsMod/Content/NPCs/Enemy/Chapter6/Oneirosbomb",1,visibility);return;}
        if(p.ModProjectile is Iceexplode){Original(p,"ArknightsMod/Content/NPCs/Enemy/Chapter6/Iceexplode",5,visibility);return;}
        if(p.ModProjectile is BlackIceProjectile){Original(p,"BlackIceProjectile",6,visibility);return;}
        if(p.ModProjectile is BlackIceIntro){Original(p,"BlackIceIntro",15,visibility);return;}
        if(p.ModProjectile is BlackIceExplode){Original(p,"BlackIceExplode",6,visibility);return;}
        if(p.ModProjectile is BlackIce or FrostNovaBlackIceSpike){Original(p,"BlackIce",1,visibility);return;}
        if(p.ModProjectile is BlizzardStorm)
        {
            Texture2D texture=Native(ProjectileID.Sharknado);int height=texture.Height/6;
            Rectangle frame=new(0,(int)(Main.GameUpdateCount/5%6)*height,texture.Width,height);
            for(int i=0;i<12;i++)Main.spriteBatch.Draw(texture,p.Center+new Vector2(MathF.Sin(Main.GlobalTimeWrappedHourly*4+i*.5f)*7,i*17-110)-Main.screenPosition,
                frame,new Color(180,230,255)*(visibility*.65f),0,frame.Size()/2,new Vector2(80-i*4,27)/frame.Size(),SpriteEffects.None,0);
            return;
        }
        bool black=p.ModProjectile is FrostNovaBarrage barrage && barrage.IsSecondPhase;
        if(p.ModProjectile is FrostNovaBarrage && p.ai[2]>0)visibility*=.3f+.5f*(1-Math.Clamp(p.ai[2]/60f,0,1));
        for(int i=Math.Min(8,p.oldPos.Length-1);i>0;i-=2)
            if(p.oldPos[i]!=Vector2.Zero)Crystal(p.oldPos[i]+p.Size/2,new Vector2(12,23),p.rotation,visibility*.045f*(9-i),black);
        Crystal(p.Center,new Vector2(14,28),p.rotation,visibility,black);
    }
}
