using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;
// Flint timing: 30 held frames, a five-frame release and 15-frame recovery.
// The visible hammer uses vanilla's item-swing pose in both weapon/tool modes.
public sealed class EarthshakerHeldHammer : ModProjectile
{
    private int phase,age,charge,direction=1,status,pulses=1,threshold=30;
    private float range=1;
    public bool IsCharging=>phase==0;
    public float ChargeProgress=>Math.Clamp(charge/(float)threshold,0,1);
    internal EarthshakerKind Kind=>(EarthshakerKind)(int)Projectile.ai[0];
    public override string Texture=>"Terraria/Images/MagicPixel";
    internal void Configure(float radius,int effect,int strikes){range=radius;status=effect;pulses=strikes;Projectile.netUpdate=true;}
    public static EarthshakerHeldHammer Find(Player p){foreach(Projectile q in Main.ActiveProjectiles)if(q.owner==p.whoAmI&&q.ModProjectile is EarthshakerHeldHammer h)return h;return null;}
    public override void SetDefaults(){Projectile.width=Projectile.height=20;Projectile.timeLeft=2;Projectile.penetrate=-1;Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.netImportant=true;}
    public override bool ShouldUpdatePosition()=>false;
    public override bool? CanDamage()=>false;
    public override void AI()
    {
        Player p=Main.player[Projectile.owner];bool local=p.whoAmI==Main.myPlayer;
        if(!p.active||p.dead||p.noItems||p.CCed||p.selectedItem!=(int)Projectile.ai[1]||p.HeldItem.ModItem is not EarthshakerWeapon w||w.Kind!=Kind
            ||local&&p.GetModPlayer<EarthshakerPlayer>().RightHeld){Projectile.Kill();return;}
        Projectile.timeLeft=2;Projectile.Center=p.MountedCenter;p.heldProj=Projectile.whoAmI;
        if(phase==0){
            if(local){int wanted=Main.MouseWorld.X>=p.MountedCenter.X?1:-1;if(wanted!=direction){direction=wanted;Projectile.netUpdate=true;}
                threshold=p.GetModPlayer<EarthshakerPlayer>().ChargeTicks;if(!p.channel)Release(p);}
            if(phase==0){
                charge++;p.ChangeDir(direction);p.itemAnimationMax=15;p.SetDummyItemTime(15+Math.Min(charge,3));
                if(charge==threshold&&!Main.dedServ)SoundEngine.PlaySound(SoundID.Item29 with{Pitch=.4f,Volume=.65f},p.Center);
                if(!Main.dedServ&&charge%5==0){var d=Dust.NewDustPerfect(p.itemLocation+new Vector2(41*p.direction,-41*p.gravDir).RotatedBy(p.itemRotation),DustID.Torch,new Vector2(0,-3*p.gravDir),0,default,1.2f);d.noGravity=true;}
                return;
            }
        }
        p.ChangeDir(direction);age++;if(age>=15){Projectile.Kill();return;}
        p.SetDummyItemTime(Math.Max(2,(int)MathHelper.Lerp(15,0,Math.Clamp(age/5f,0,1))));
    }
    private void Release(Player p)
    {
        bool full=charge>=threshold;phase=1;age=0;Projectile.netUpdate=true;
        int slash=Projectile.NewProjectile(Projectile.GetSource_FromThis(),p.MountedCenter,new Vector2(direction,0),ModContent.ProjectileType<EarthshakerFlintSlash>(),
            Math.Max(1,(int)(Projectile.damage*1.78f)),Projectile.knockBack,p.whoAmI,(int)Kind,direction*p.gravDir,range);
        if(slash<Main.maxProjectiles&&Main.projectile[slash].ModProjectile is EarthshakerFlintSlash s)s.Status=status;
        if(full){int id=Projectile.NewProjectile(Projectile.GetSource_FromThis(),p.MountedCenter+new Vector2(direction*30,0),Vector2.Zero,
            ModContent.ProjectileType<EarthshakerFlameLine>(),Projectile.damage,Projectile.knockBack,p.whoAmI,(int)Kind,direction,p.gravDir);
            if(id<Main.maxProjectiles&&Main.projectile[id].ModProjectile is EarthshakerFlameLine line)line.Configure(range,status,pulses);}
        p.GetModPlayer<EarthshakerPlayer>().Released();
        if(!full&&!Main.dedServ)SoundEngine.PlaySound(SoundID.Item1 with{Volume=.6f},p.Center);
    }
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void SendExtraAI(BinaryWriter w){w.Write((byte)phase);w.Write(age);w.Write(charge);w.Write((sbyte)direction);w.Write(threshold);w.Write(range);w.Write((byte)status);w.Write((byte)pulses);}
    public override void ReceiveExtraAI(BinaryReader r){phase=r.ReadByte();age=r.ReadInt32();charge=r.ReadInt32();direction=r.ReadSByte();threshold=Math.Clamp(r.ReadInt32(),1,60);range=Math.Clamp(r.ReadSingle(),1,2);status=r.ReadByte();pulses=Math.Clamp((int)r.ReadByte(),1,5);}
}
