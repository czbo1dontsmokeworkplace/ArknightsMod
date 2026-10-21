using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

public sealed class ReaperWave : ModProjectile
{
    private int age,hits;
    public override string Texture => "Terraria/Images/MagicPixel";
    internal static int Spawn(Player owner, IEntitySource source, Vector2 point, Vector2 velocity, int damage, float knockback, ReaperKind kind)
        => Projectile.NewProjectile(source, point, velocity, ModContent.ProjectileType<ReaperWave>(), Math.Max(1, damage), knockback, owner.whoAmI, (int)kind, owner.selectedItem);
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=100;Projectile.friendly=true;Projectile.DamageType=DamageClass.Melee;
        Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.timeLeft=90;Projectile.penetrate=7;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;
    }
    public override void OnSpawn(IEntitySource source){Projectile.rotation=Projectile.velocity.ToRotation();}
    public override bool? CanDamage()=>hits<7?null:false;
    public override void AI()
    {
        // Fixed heading, no target searches. Accelerate briefly, then brake to a stop.
        if(age++<12)Projectile.velocity*=1.02f;else Projectile.velocity*=.9f;
        if(Projectile.velocity.LengthSquared()<.12f*.12f)Projectile.velocity=Vector2.Zero;
        Projectile.Opacity=Math.Min(1,age/4f)*Math.Min(1,Projectile.timeLeft/18f);
        if(age%4==0)ReaperVisuals.Shed(Projectile.Center,-Projectile.rotation.ToRotationVector2(),(ReaperKind)(int)Projectile.ai[0]);
    }
    public override bool? Colliding(Rectangle a,Rectangle b)
    {
        Vector2 point=b.ClosestPointInRect(Projectile.Center),offset=point-Projectile.Center;
        if(offset.LengthSquared()>90*90*Projectile.scale*Projectile.scale)return false;
        return offset.LengthSquared()<30*30||Math.Abs(MathHelper.WrapAngle(offset.ToRotation()-Projectile.rotation))<1.49f;
    }
    public override void OnHitNPC(NPC target,NPC.HitInfo hit,int damageDone)
    {
        if(hits>=7)return;
        hits++;Projectile.velocity*=.9f;Projectile.damage=Math.Max(1,(int)(Projectile.damage*.96f));Projectile.netUpdate=true;
        ReaperVisuals.Hit(target.Center,(ReaperKind)(int)Projectile.ai[0],.65f);
        if(hits>=7)Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor){ReaperVisuals.DrawWave(Projectile);return false;}
    public override void SendExtraAI(System.IO.BinaryWriter w){w.Write(age);w.Write((byte)hits);}
    public override void ReceiveExtraAI(System.IO.BinaryReader r){age=Math.Max(0,r.ReadInt32());hits=Math.Clamp((int)r.ReadByte(),0,7);}
}
