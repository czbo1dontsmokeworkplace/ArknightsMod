using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

public sealed class ReaperSwing : ModProjectile
{
    private float age, duration = ReaperRules.SwingTicks, aim, radius, previousAngle, fromAngle, toAngle;
    private int facing = 1, sequence;
    private bool started, released;
    private Item boundItem;
    private readonly Vector2[] history = new Vector2[18];
    private int samples;
    private Vector2 grip, tip, previousCenter, fromCenter, toCenter;
    internal ReaperKind Kind => (ReaperKind)(int)Projectile.ai[0];
    internal float Progress => Math.Clamp(age / duration, 0, 1);
    // Nine timed poses from the supplied 9 x 100ms reference: a deliberate windup,
    // rapid front cut and decelerating follow-through. Never draw the reference GIF.
    private static readonly float[] poses = [-2.35f, -2.22f, -2.06f, -1.74f, -.15f, 1.16f, 2.15f, 2.67f, 2.87f];
    internal static float Turn(float t)
    {
        float p = Math.Clamp(t, 0, 1) * 8; int i = Math.Min(7, (int)p); float u = p - i;
        float a = poses[i], b = poses[i + 1];
        float m0 = i == 0 ? b-a : (b-poses[i-1])*.5f;
        float m1 = i == 7 ? b-a : (poses[i+2]-a)*.5f;
        return (2*u*u*u-3*u*u+1)*a+(u*u*u-2*u*u+u)*m0+(-2*u*u*u+3*u*u)*b+(u*u*u-u*u)*m1;
    }
    private float Angle => aim + Turn(Progress) * facing;
    public override string Texture => "Terraria/Images/MagicPixel";
    public static ReaperSwing Find(Player p)
    {
        foreach (Projectile q in Main.ActiveProjectiles) if(q.owner==p.whoAmI && q.ModProjectile is ReaperSwing swing)return swing;
        return null;
    }
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=12; Projectile.friendly=true; Projectile.DamageType=DamageClass.Melee;
        Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.penetrate=-1;Projectile.timeLeft=2;Projectile.extraUpdates=2;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;Projectile.netImportant=true;
    }
    public override bool ShouldUpdatePosition()=>false;
    private void Begin(Player p)
    {
        var state=p.GetModPlayer<ReaperPlayer>();state.BeginAttack();
        duration=Math.Max(12,ReaperRules.SwingTicks/(p.GetTotalAttackSpeed(DamageClass.Melee)*state.AttackSpeed));
        radius=ReaperRules.Reach(Kind)*state.ReachMultiplier*p.GetAdjustedItemScale(p.HeldItem);
        aim=(Main.MouseWorld-p.MountedCenter).SafeNormalize(new Vector2(p.direction,0)).ToRotation();
        facing=(MathF.Cos(aim)>=0?1:-1)*(p.gravDir<0?-1:1);
        age=0;samples=0;released=false;started=true;sequence++;Array.Clear(Projectile.localNPCImmunity);
        Projectile.damage=Math.Max(1,(int)(p.GetWeaponDamage(p.HeldItem)*state.AttackPower));Projectile.CritChance=p.GetWeaponCrit(p.HeldItem);
        previousAngle=fromAngle=toAngle=Angle;previousCenter=fromCenter=toCenter=p.MountedCenter;Projectile.netUpdate=true;
    }
    public override void AI()
    {
        Player p=Main.player[Projectile.owner];bool local=p.whoAmI==Main.myPlayer;var state=p.GetModPlayer<ReaperPlayer>();
        if(!p.active||p.dead||p.noItems||p.CCed||p.selectedItem!=(int)Projectile.ai[1]
            ||p.HeldItem.ModItem is not ReaperWeapon w||w.Kind!=Kind||state.SuppressAttacks
            ||local&&(state.RightHeld||boundItem!=null&&!ReferenceEquals(boundItem,p.HeldItem))){Projectile.Kill();return;}
        boundItem??=p.HeldItem;
        if(!started){if(!local){Projectile.timeLeft=2;return;}Begin(p);}
        if(age>=duration){if(!local){Projectile.timeLeft=2;return;}
            if(!p.controlUseItem||!ChargedOperatorWeapon.WorldInputAllowed(p)||ArknightsKeybinds.SkillActivatePressed(p)){Projectile.Kill();return;}Begin(p);}
        Projectile.timeLeft=2;Projectile.Center=grip=p.MountedCenter;
        float before=Progress;age=BladeSwingTemplate.Advance(age,duration,Projectile.extraUpdates);
        tip=grip+Angle.ToRotationVector2()*radius;
        if(samples>0&&Vector2.DistanceSquared(history[0],grip)>500*500)samples=0;
        for(int i=Math.Min(samples,history.Length-1);i>0;i--)history[i]=history[i-1];
        history[0]=tip;samples=Math.Min(samples+1,history.Length);
        fromAngle=previousAngle;toAngle=Angle;fromCenter=previousCenter;toCenter=grip;
        p.ChangeDir(MathF.Cos(aim)>=0?1:-1);p.heldProj=Projectile.whoAmI;p.itemTime=p.itemAnimation=2;BladeSwingTemplate.Pose(p,Angle);
        if(before<.43f&&Progress>=.43f&&!Main.dedServ)SoundEngine.PlaySound(SoundID.Item71 with{Volume=.6f,Pitch=.1f},grip);
        if(!released&&Progress>=.51f){released=true;if(local){ReaperWave.Spawn(p,Projectile.GetSource_FromThis(),grip+aim.ToRotationVector2()*30,
            aim.ToRotationVector2()*14,Math.Max(1,(int)(Projectile.damage*.65f)),Projectile.knockBack*.6f,Kind);Projectile.netUpdate=true;}}
        if(Projectile.numUpdates==0&&Progress>.3f&&Progress<.82f)ReaperVisuals.CutMotes(grip,tip,(Angle+facing*MathHelper.PiOver2).ToRotationVector2(),Kind);
        previousAngle=Angle;previousCenter=grip;
    }
    public override bool? CanDamage()=>started&&Progress>=.3f&&Progress<=.86f?null:false;
    public override bool? Colliding(Rectangle a,Rectangle b)=>CanDamage()!=false&&BladeSwingTemplate.Contact(b,fromAngle,toAngle,
        fromCenter,toCenter,radius,26,facing,out _,out _);
    public override void ModifyHitNPC(NPC target,ref NPC.HitModifiers modifiers)=>Main.player[Projectile.owner].GetModPlayer<ReaperPlayer>().ModifyCut(target,ref modifiers);
    public override void OnHitNPC(NPC target,NPC.HitInfo hit,int damageDone)
    {
        Main.player[Projectile.owner].GetModPlayer<ReaperPlayer>().OnCut(target,damageDone,true);
        ReaperVisuals.Hit(target.Hitbox.ClosestPointInRect(tip),Kind,.8f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if(!started)return false;
        float glow=Math.Clamp((Progress-.3f)/.1f,0,1)*Math.Clamp((.94f-Progress)/.2f,0,1);
        ReaperMaterial.Trail(grip,history,samples,Kind,glow);
        ReaperVisuals.Blade(grip,tip,Kind,1);return false;
    }
    public override void SendExtraAI(BinaryWriter w){w.Write(started);w.Write(age);w.Write(duration);w.Write(aim);w.Write(radius);w.Write(facing);w.Write(released);w.Write(Projectile.damage);w.Write(sequence);}
    public override void ReceiveExtraAI(BinaryReader r){started=r.ReadBoolean();age=r.ReadSingle();duration=Math.Clamp(r.ReadSingle(),12,600);aim=r.ReadSingle();radius=Math.Clamp(r.ReadSingle(),1,500);facing=r.ReadInt32();released=r.ReadBoolean();Projectile.damage=r.ReadInt32();int next=r.ReadInt32();if(next!=sequence)Array.Clear(Projectile.localNPCImmunity);sequence=next;
        samples=0;previousAngle=fromAngle=toAngle=Angle;previousCenter=fromCenter=toCenter=Main.player[Projectile.owner].MountedCenter;}
}
