using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

public class ExusiaiVector_Bullet : ModProjectile
{
    internal const int TotalUpdates=8;
    internal const float MuzzleStep=16f*7/TotalUpdates;
    private Vector2 contactPoint,contactReverse;
    private bool contact,wall;
    private PhoenixMotionFx dragonfly;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.timeLeft = (int)MathF.Ceiling(240f*TotalUpdates/7);
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true; Projectile.ignoreWater = true;
        Projectile.extraUpdates = TotalUpdates-1; Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ && dragonfly==null)
        {
            var style=PhoenixFx.Original();style.SpriteStretch=new Vector2(1,2);
            dragonfly=new PhoenixMotionFx(45,Projectile,style);
        }
        if (Projectile.numUpdates == 0)
            Lighting.AddLight(Projectile.Center, VectorArt.RoundColor.ToVector3() * .3f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // Twice the longitudinal light footprint; 14px x 8 steps still travels
        // 112px per game tick, exactly matching the previous 16px x 7.
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
        dragonfly?.Stop();
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
        PhoenixFx.Burst("emitter.player.weapon.45.impact.sparks",Projectile.Center,
            Projectile.velocity.SafeNormalize(Vector2.UnitX));
    }
    public override bool PreDraw(ref Color lightColor)
    {
        // The source impact graph is drawn by the shared renderer.
        return false;
    }
}
