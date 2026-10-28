using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

public class ExusiaiVector_Bullet : ModProjectile
{
    private Vector2 launch;
    private bool initialized;
    private Vector2 trailAnchor,contactPoint,contactReverse;
    private int sample;
    private bool contact,wall;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.timeLeft = 240;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 6; Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (!initialized) { initialized = true; launch = trailAnchor = Projectile.Center; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ) {
            Vector2 delta=Projectile.Center-trailAnchor;
            int count=Math.Min(10,(int)(delta.Length()/16));
            Vector2 step=delta.SafeNormalize(Vector2.UnitX)*16;
            for(int i=0;i<count;i++){trailAnchor+=step;VectorVisualEffects.Flight(trailAnchor,Projectile.velocity,sample++);}
        }
        if (Projectile.numUpdates == 0)
            Lighting.AddLight(Projectile.Center, VectorArt.RoundColor.ToVector3() * .3f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        VectorArt.Round(Main.spriteBatch, Projectile.Center - Main.screenPosition, Projectile.velocity,
            initialized ? Vector2.Distance(launch, Projectile.Center) : 0, (int)Projectile.ai[0]);
        return false;
    }
    public override void OnHitNPC(NPC target,NPC.HitInfo hit,int damageDone)
    {
        contact=true;
        contactPoint=Vector2.Clamp(Projectile.Center,target.position,target.position+target.Size);
        contactReverse=-Projectile.velocity.SafeNormalize(Vector2.UnitX);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        contact=wall=true;contactPoint=Projectile.Center;
        contactReverse=-oldVelocity.SafeNormalize(Vector2.UnitX);return true;
    }
    public override void OnKill(int timeLeft)
    {
        // One compact, networked visual burst; decoration never creates damaging projectiles.
        if (contact && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), contactPoint,
                contactReverse, ModContent.ProjectileType<VectorImpact>(),
                0, 0, Projectile.owner, Projectile.ai[0], Projectile.identity % 127,wall?1:0);
    }
}

public sealed class VectorImpact : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 15;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Main.dedServ || Projectile.localAI[0]++ != 0) return;
        VectorVisualEffects.Contact(Projectile.Center,Projectile.velocity.SafeNormalize(Vector2.UnitX),Projectile.ai[2]>0);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        VectorArt.Impact(Main.spriteBatch, Projectile.Center - Main.screenPosition, Projectile.velocity,
            1 - Projectile.timeLeft / 15f, (int)Projectile.ai[1], (int)Projectile.ai[0]);
        return false;
    }
}
