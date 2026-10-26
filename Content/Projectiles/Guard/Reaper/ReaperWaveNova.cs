using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

// UnholyEssence_HolyNova flight, separate from the existing right-click ReaperWisp.
// No ReaperPlayer hit callbacks: these children never heal or recover skill points.
public sealed class ReaperWaveNova : ModProjectile
{
    private ref float Timer => ref Projectile.ai[1];
    private ReaperKind Kind => (ReaperKind)Math.Clamp((int)Projectile.ai[0],0,2);
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=30;Projectile.friendly=true;Projectile.hostile=false;
        Projectile.DamageType=DamageClass.Melee;Projectile.penetrate=1;Projectile.timeLeft=150;
        Projectile.tileCollide=false;Projectile.ignoreWater=true;Projectile.extraUpdates=1;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=12;
    }
    public override void AI()
    {
        Timer++;
        NPC target=null;float nearest=1200;
        foreach(NPC npc in Main.ActiveNPCs) {
            if(!npc.CanBeChasedBy(Projectile))continue;
            float distance=Projectile.Distance(npc.Center);
            if(distance>=nearest)continue;
            nearest=distance;target=npc;
        }
        Vector2 direction=Projectile.velocity.SafeNormalize(Vector2.UnitX);
        if(target!=null) {
            Vector2 desired=(target.Center-Projectile.Center).SafeNormalize(direction);
            float tracking=Math.Clamp(Timer/50,0,1),speed=MathHelper.Lerp(10,18,tracking);
            Projectile.velocity=Vector2.Lerp(Projectile.velocity,desired*speed,MathHelper.Lerp(.08f,.36f,tracking));
        }
        else Projectile.velocity*=.992f;
        Projectile.rotation=Projectile.velocity.ToRotation();
        if(!Main.dedServ) {
            Lighting.AddLight(Projectile.Center,ReaperRules.Color(Kind).ToVector3()*.52f);
            if(Projectile.numUpdates==0)Trail(direction);
        }
    }
    private void Trail(Vector2 direction)
    {
        if(!ReaperVisuals.Visible(Projectile.Center))return;
        if(ParticleManager.activeParticles.Count<1100)
            new T_Reaper_WavePulse(Projectile.Center,-direction*.8f,ReaperRules.Color(Kind),16,32,10).Spawn();
        Vector2 side=direction.RotatedBy(MathHelper.PiOver2);
        Dust dust=Dust.NewDustPerfect(Projectile.Center-direction*Main.rand.NextFloat(3,12)+side*Main.rand.NextFloat(-6,6),
            DustID.GoldFlame,-direction*Main.rand.NextFloat(.6f,2)+side*Main.rand.NextFloat(-.3f,.3f),
            0,ReaperRules.Edge(Kind),Main.rand.NextFloat(.75f,1.2f));
        dust.noGravity=true;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if(ReaperVisuals.Visible(Projectile.Center)) {
            float pulse=.9f+MathF.Sin(Timer*.24f)*.1f;
            ReaperWaveNovaVisuals.Bloom(Projectile.Center,new Vector2(88*pulse),ReaperRules.Color(Kind)*.42f,Projectile.rotation);
        }
        return false;
    }
}
