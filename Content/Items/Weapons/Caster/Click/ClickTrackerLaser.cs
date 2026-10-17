using System;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class ClickTrackerLaser : ModProjectile
{
    public override string Texture=>"ArknightsMod/Common/Particle/DefaultParticle";
    private Vector2 start;
    private int targetType;
    private bool ready, sparked, empowered;
    private int targetCount=1;
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=4;Projectile.friendly=true;Projectile.DamageType=DamageClass.Magic;
        Projectile.penetrate=-1;Projectile.timeLeft=15;Projectile.tileCollide=false;Projectile.ignoreWater=true;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;
    }
    internal static void Spawn(Projectile drone,NPC target,int damage,bool laser,bool stun,int count,bool burst = false)
    {
        int id=Projectile.NewProjectile(drone.GetSource_FromThis(),target.Center,Vector2.Zero,
            ModContent.ProjectileType<ClickTrackerLaser>(),damage,0,drone.owner,target.whoAmI,
            laser?drone.identity+1:0,stun?1:0);
        if(id>=Main.maxProjectiles)return;
        var shot=(ClickTrackerLaser)Main.projectile[id].ModProjectile;
        shot.empowered=burst;shot.start=drone.Center;shot.targetType=target.type;shot.targetCount=Math.Clamp(count,1,80);shot.ready=true;shot.Projectile.netUpdate=true;
    }
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>ready&&Projectile.numHits==0?null:false;
    public override bool? CanHitNPC(NPC target)=>target.whoAmI==(int)Projectile.ai[0]&&target.type==targetType?null:false;
    public override void SendExtraAI(BinaryWriter w){w.WriteVector2(start);w.Write(targetType);w.Write(ready);w.Write(empowered);w.Write((byte)targetCount);}
    public override void ReceiveExtraAI(BinaryReader r)
    {start=r.ReadVector2();targetType=r.ReadInt32();ready=r.ReadBoolean()&&float.IsFinite(start.X)&&float.IsFinite(start.Y);empowered=r.ReadBoolean();targetCount=Math.Clamp((int)r.ReadByte(),1,80);}
    public override void AI()
    {
        if(!ready)return;
        int index=(int)Projectile.ai[0];
        if(index<0||index>=Main.maxNPCs||!Main.npc[index].active||Main.npc[index].type!=targetType)
        {Projectile.friendly=false;return;}
        Projectile.Center=Main.npc[index].Center;
        if(Projectile.ai[1]>0)
            foreach(Projectile p in Main.ActiveProjectiles)
                if(p.owner==Projectile.owner&&p.identity==(int)Projectile.ai[1]-1&&p.ModProjectile is ClickDrone)
                {start=p.Center;break;}
        if(!sparked&&!Main.dedServ)
        {
            sparked=true;
            CasterSparkles.Spawn(Projectile.Center,Vector2.Zero,Color.White,.55f,12);
            CasterSparkles.Spawn(Projectile.Center,new Vector2(0,-.25f),new Color(255,100,110),.35f,15,MathHelper.PiOver4);
        }
    }
    public override void OnHitNPC(NPC target,NPC.HitInfo hit,int damageDone)
    {
        if(Projectile.ai[2]>0&&!target.boss&&target.realLife<0&&Main.rand.NextBool(5))
        {
            if(Main.netMode==NetmodeID.MultiplayerClient)target.AddBuff(ModContent.BuffType<StunDebuff>(),24);
            else OperatorStunNPC.TryApply(target,24);
        }
    }
    // AerialTracker: instantaneous endpoint, tapered red/white beam, cubic 15-tick fade.
    // Rebuilt from local neutral masks: no Calamity assets, laser-burn state, or runtime dependency.
    internal static void DrawBeam(Vector2 from,Vector2 to,float fade,int count,Color? beamColor=null,float thickness=1f)
    {
        Vector2 delta=to-from;float distance=delta.Length();if(distance>1800||distance<1)return;
        using var pass=new MechAccordVfx.Pass(BlendState.Additive);
        float intensity=1f/MathF.Sqrt(Math.Max(1,count*.4f));
        Vector2 direction=delta/distance;float angle=direction.ToRotation();
        for(float i=10;i<distance-10;i+=10)
        {
            float width=MathHelper.Lerp(.8f,3f,1-i/distance)*Math.Max(fade,.3f)*thickness;
            MechAccordVfx.Mask("Bloom",from+direction*i,new Vector2(24,width*5),
                MechAccordVfx.Fade(beamColor ?? new Color(243,11,33),fade*intensity),angle);
            MechAccordVfx.Mask("Bloom",from+direction*i,new Vector2(24,width*1.5f),
                MechAccordVfx.Fade(Color.White,fade*intensity),angle);
        }
    }
    public override void DrawBehind(int index,System.Collections.Generic.List<int> a,System.Collections.Generic.List<int> b,
        System.Collections.Generic.List<int> c,System.Collections.Generic.List<int> overPlayers,System.Collections.Generic.List<int> e)=>overPlayers.Add(index);
    public override bool PreDraw(ref Color lightColor)
    {if(ready&&Projectile.ai[1]>0)DrawBeam(start,Projectile.Center,MathF.Pow(Projectile.timeLeft/15f,3),targetCount,null,empowered?2f:1f);return false;}
}
