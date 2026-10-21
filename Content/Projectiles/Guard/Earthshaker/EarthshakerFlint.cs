using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;

internal static class FlintContact
{
    internal static void Hit(Projectile q,NPC target,int status)
    {
        Main.player[q.owner].GetModPlayer<EarthshakerPlayer>().CreditAttack();
        if(target.boss||target.knockBackResist<=0||target.realLife>=0)return;
        if(status==1)target.velocity.Y=Math.Min(target.velocity.Y,-6*target.knockBackResist);
        if(status>=2)target.AddBuff(ModContent.BuffType<EarthshakerStagger>(),status==3?35:20);
    }
}

public sealed class EarthshakerFlintSlash:ModProjectile
{
    private int age,hits;private float oldAngle,angle;private Vector2 oldCenter;
    public int Status;
    public override string Texture=>"ArknightsMod/Content/Projectiles/Guard/Earthshaker/FlintSlash";
    public override void SetDefaults(){Projectile.width=Projectile.height=16;Projectile.friendly=true;Projectile.DamageType=DamageClass.Melee;Projectile.tileCollide=false;
        Projectile.ignoreWater=true;Projectile.penetrate=-1;Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;Projectile.timeLeft=7;}
    public override bool ShouldUpdatePosition()=>false;
    public override void AI()
    {
        Player p=Main.player[Projectile.owner];if(!p.active||p.dead){Projectile.Kill();return;}
        if(age==0){oldAngle=Projectile.velocity.ToRotation()+MathHelper.Pi*Projectile.ai[1];oldCenter=p.MountedCenter;}
        oldAngle=age==0?oldAngle:angle;oldCenter=Projectile.Center;
        if(age==0)oldCenter=p.MountedCenter;
        age++;if(age>=6){Projectile.Kill();return;}
        float t=age/6f;angle=Projectile.velocity.ToRotation()+MathHelper.Pi*Projectile.ai[1]*(1+t);
        Projectile.rotation=angle;Projectile.Center=p.MountedCenter;Projectile.scale=(1+t*.2f)*Projectile.ai[2];
    }
    // 1.4.5 Flint keeps its six-frame arc after its two damaging contacts.
    public override bool? CanDamage()=>hits<2?null:false;
    public override bool? Colliding(Rectangle a,Rectangle b)=>hits<2&&BladeSwingTemplate.Contact(b,oldAngle,angle,oldCenter,Projectile.Center,
        84*Projectile.scale,26,(int)Projectile.ai[1],out _,out _);
    public override void OnHitNPC(NPC n,NPC.HitInfo h,int d){hits++;Projectile.netUpdate=true;FlintContact.Hit(Projectile,n,Status);}
    public override bool PreDraw(ref Color lightColor)
    {
        var texture=ModContent.Request<Texture2D>(Texture,AssetRequestMode.ImmediateLoad).Value;
        Rectangle frame=texture.Frame(1,4);Vector2 origin=frame.Size()*.5f;
        float t=age/6f,fade=Math.Clamp(t/.6f,0,1)*Math.Clamp((1-t)/.4f,0,1);
        SpriteEffects effects=Projectile.ai[1]>=0?SpriteEffects.None:SpriteEffects.FlipVertically;
        Vector2 at=Projectile.Center-Main.screenPosition;float scale=Projectile.scale*1.1f;
        Main.EntitySpriteDraw(texture,at,frame,new Color(227,177,140)*fade,Projectile.rotation-Projectile.ai[1]*MathHelper.PiOver4*(1-t),origin,scale*.975f,effects);
        Main.EntitySpriteDraw(texture,at,frame,new Color(255,230,120)*(fade*.7f),Projectile.rotation,origin,scale*.975f,effects);
        Main.EntitySpriteDraw(texture,at,frame,Color.White*(fade*.3f),Projectile.rotation,origin,scale*.8f,effects);
        Main.EntitySpriteDraw(texture,at,texture.Frame(1,4,0,3),Color.White*(fade*.15f),Projectile.rotation,origin,scale,effects);return false;
    }
    public override void SendExtraAI(BinaryWriter w){w.Write((byte)Status);w.Write((byte)hits);}
    public override void ReceiveExtraAI(BinaryReader r){Status=r.ReadByte();hits=Math.Clamp((int)r.ReadByte(),0,2);}
}

public sealed class EarthshakerFlameLine:ModProjectile
{
    private int age,status,pulses=1;private float range=1;
    public override string Texture=>"Terraria/Images/MagicPixel";
    public void Configure(float r,int s,int p){range=r;status=s;pulses=p;Projectile.netUpdate=true;}
    public override void SetDefaults(){Projectile.width=Projectile.height=2;Projectile.timeLeft=60;Projectile.tileCollide=false;Projectile.netImportant=true;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>false;
    internal static bool Walkable(int x,int y)
    {
        if(x<10||x>=Main.maxTilesX-10||y<10||y>=Main.maxTilesY-10)return false;
        Tile tile=Main.tile[x,y];return tile.HasTile&&!tile.IsActuated&&(Main.tileSolid[tile.TileType]||Main.tileSolidTop[tile.TileType]);
    }
    internal static bool FindSurface(int x,ref int y,float gravity)
    {
        if(x<10||x>=Main.maxTilesX-10)return false;
        int sign=gravity<0?-1:1;y=Math.Clamp(y,10,Main.maxTilesY-11);
        for(int i=0;i<10&&WorldGen.SolidTile(x,y);i++)y=Math.Clamp(y-sign,10,Main.maxTilesY-11);
        for(int i=0;i<10&&!Walkable(x,y);i++)y=Math.Clamp(y+sign,10,Main.maxTilesY-11);
        return Walkable(x,y);
    }
    public override void AI()
    {
        if(age%10==0&&age/10<pulses&&Projectile.owner==Main.myPlayer)Emit(age/10);
        if(++age>=Math.Max(1,pulses)*10)Projectile.Kill();
    }
    private void Emit(int pulse)
    {
        Point p=Projectile.Center.ToTileCoordinates();int direction=(int)Projectile.ai[1];float gravity=Projectile.ai[2];
        for(int i=0;i<=12;i++){
            if(!FindSurface(p.X,ref p.Y,gravity))break;
            Vector2 foot=new(p.X*16+8,p.Y*16+(gravity<0?16:0));
            Vector2 velocity=new Vector2(direction,-10*gravity).RotatedBy(Main.rand.NextFloat(-1,1)*MathHelper.TwoPi*.025f);
            float scale=(1+i*.04f+Main.rand.NextFloat()*.2f)*range;
            int id=Projectile.NewProjectile(Projectile.GetSource_FromThis(),foot,velocity,ModContent.ProjectileType<EarthshakerFlintFlame>(),
                Math.Max(1,(int)(Projectile.damage*(1+pulse*.1f))),Projectile.knockBack,Projectile.owner,-i*2,scale,Projectile.ai[0]);
            if(id<Main.maxProjectiles&&Main.projectile[id].ModProjectile is EarthshakerFlintFlame flame)flame.Status=status;
            if(i==0&&pulse==0){EarthshakerVisuals.Impact(foot,(EarthshakerKind)(int)Projectile.ai[0],1,150,true,gravity);
                Main.player[Projectile.owner].GetModPlayer<EarthshakerPlayer>().Shake(5);}
            p.X+=direction*2;
        }
    }
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void SendExtraAI(BinaryWriter w){w.Write(range);w.Write((byte)status);w.Write((byte)pulses);w.Write(age);}
    public override void ReceiveExtraAI(BinaryReader r){range=Math.Clamp(r.ReadSingle(),1,2);status=r.ReadByte();pulses=Math.Clamp((int)r.ReadByte(),1,5);age=Math.Max(0,r.ReadInt32());}
}

public sealed class EarthshakerFlintFlame:ModProjectile
{
    private bool ignited;public int Status;
    public override string Texture=>"ArknightsMod/Content/Projectiles/Guard/Earthshaker/FlintFlame";
    public override void SetDefaults(){Projectile.width=Projectile.height=32;Projectile.friendly=true;Projectile.DamageType=DamageClass.Melee;
        Projectile.alpha=255;Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.penetrate=4;Projectile.timeLeft=90;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=25;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>Projectile.ai[0]>=0&&Projectile.ai[0]<14?null:false;
    public override void AI()
    {
        float t=Projectile.ai[0]++;if(t<0){Projectile.alpha=255;Projectile.scale=.1f*Projectile.ai[1];return;}
        if(!ignited){ignited=true;Projectile.rotation=Projectile.velocity.ToRotation();
            EarthshakerVisuals.FlameBurst(Projectile);}
        if(++Projectile.frameCounter>=(Projectile.frame==0?6:2)){Projectile.frameCounter=0;Projectile.frame=Math.Min(7,Projectile.frame+1);}
        if(t<4){Projectile.Opacity+=.1f;Projectile.scale=Projectile.Opacity*Projectile.ai[1];}
        if(t>=14)Projectile.Opacity-=.2f;
        if(t>=19){Projectile.Kill();return;}
        if(!Main.dedServ)Lighting.AddLight(Projectile.Center,new Vector3(1,.45f,.1f)*Projectile.Opacity);
    }
    public override bool? Colliding(Rectangle a,Rectangle b)
    {
        if(CanDamage()==false)return false;float distance=0;
        return Collision.CheckAABBvLineCollision(b.TopLeft(),b.Size(),Projectile.Center,Projectile.Center+Projectile.rotation.ToRotationVector2()*180*Projectile.scale,
            32*Projectile.scale,ref distance);
    }
    public override void OnHitNPC(NPC n,NPC.HitInfo hit,int damage){FlintContact.Hit(Projectile,n,Status);n.AddBuff(BuffID.OnFire,180);}
    public override bool PreDraw(ref Color lightColor)
    {
        if(!ignited)return false;
        var texture=ModContent.Request<Texture2D>(Texture,AssetRequestMode.ImmediateLoad).Value;
        var frame=texture.Frame(1,8,0,Projectile.frame);
        Main.EntitySpriteDraw(texture,Projectile.Center-Main.screenPosition,frame,Color.White,Projectile.rotation,new Vector2(16,frame.Height*.5f),Projectile.scale,SpriteEffects.None);return false;
    }
    public override void SendExtraAI(BinaryWriter w){w.Write((byte)Status);}
    public override void ReceiveExtraAI(BinaryReader r){Status=r.ReadByte();}
}
