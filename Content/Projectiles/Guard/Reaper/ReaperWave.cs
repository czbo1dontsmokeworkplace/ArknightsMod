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
    private int age,hits,novaCredit;
    public override string Texture => "Terraria/Images/MagicPixel";
    internal static int Spawn(Player owner, IEntitySource source, Vector2 point, Vector2 velocity, int damage, float knockback, ReaperKind kind)
        => Projectile.NewProjectile(source, point, velocity, ModContent.ProjectileType<ReaperWave>(), Math.Max(1, damage), knockback, owner.whoAmI, (int)kind, owner.selectedItem);
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=100;Projectile.friendly=true;Projectile.DamageType=DamageClass.Melee;
        Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.timeLeft=65;Projectile.extraUpdates=1;Projectile.penetrate=7;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;
    }
    public override void OnSpawn(IEntitySource source){Projectile.rotation=Projectile.velocity.ToRotation();}
    public override bool? CanDamage()=>hits<7?null:false;
    public override void AI()
    {
        // Apply drag once per game frame, independent of extra updates.
        if (Projectile.numUpdates == 0) Projectile.velocity *= .99f;
        age++;
        if (Projectile.numUpdates == 0) {
            // Reference: one pair / 3 game ticks. 70% = 7 pairs / 30 ticks, not one / 5.
            novaCredit += 7;
            if (novaCredit >= 30) {
                novaCredit -= 30;
                if (Projectile.owner == Main.myPlayer) ReleaseTrailingNovas();
            }
        }
        Projectile.Opacity=Math.Min(1,age/4f)*Math.Min(1,Projectile.timeLeft/18f);
        if(age%3==0)ReaperVisuals.WaveMotes(Projectile);
    }
    private void ReleaseTrailingNovas()
    {
        Vector2 forward=Projectile.velocity.SafeNormalize(Vector2.UnitX),right=forward.RotatedBy(MathHelper.PiOver2);
        for(int i=0;i<2;i++) {
            float side=(i==0?-1:1)*Main.rand.NextFloat(20,58);
            Vector2 position=Projectile.Center-forward*Main.rand.NextFloat(70,120)+right*side;
            Vector2 velocity=(-forward*Main.rand.NextFloat(3.5f,6.5f)+right*Main.rand.NextFloat(-1.4f,1.4f))
                .SafeNormalize(-forward)*Main.rand.NextFloat(7,10);
            int index=Projectile.NewProjectile(Projectile.GetSource_FromThis(),position,velocity,
                ModContent.ProjectileType<ReaperWaveNova>(),Math.Max(1,(int)(Projectile.damage*.15f)),
                Projectile.knockBack*.15f,Projectile.owner,Projectile.ai[0]);
            if(Main.projectile.IndexInRange(index))Main.projectile[index].CritChance=Projectile.CritChance;
        }
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
        hits++;Projectile.damage=Math.Max(1,(int)(Projectile.damage*.96f));Projectile.netUpdate=true;
        ReaperVisuals.Hit(target.Center,(ReaperKind)(int)Projectile.ai[0],.65f);
        if(hits>=7)Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor){ReaperVisuals.DrawWave(Projectile);return false;}
    public override void SendExtraAI(System.IO.BinaryWriter w){w.Write(age);w.Write((byte)hits);w.Write((byte)novaCredit);}
    public override void ReceiveExtraAI(System.IO.BinaryReader r){age=Math.Max(0,r.ReadInt32());hits=Math.Clamp((int)r.ReadByte(),0,7);novaCredit=Math.Clamp((int)r.ReadByte(),0,29);}
}
