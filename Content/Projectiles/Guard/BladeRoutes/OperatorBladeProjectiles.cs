using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.BladeRoutes;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

public class OperatorBladeSwing : ModProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;
    protected virtual bool Aura => false;
    public int Kind => (int)Projectile.ai[0];
    public float Progress => Projectile.localAI[0] / Math.Max(1, Projectile.ai[1]);
    private int facing;
    private float gravity;
    private bool released;
    private int spikes;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.ownerHitCheck = true;
        // Do not set usesOwnerMeleeHitCD: blade and aura must not suppress each other.
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.CCed || player.noItems || BladeRouteRules.HeldKind(player.HeldItem)!=Kind)
        { Projectile.Kill(); return; }
        if (facing == 0) { facing = Projectile.velocity.X < 0 ? -1 : 1; gravity = player.gravDir; }
        Projectile.localAI[0]++;
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
        Projectile.rotation = BladeRouteRules.Angle(Progress, facing, gravity) + player.fullRotation;
        Projectile.scale = Aura ? BladeRouteRules.AuraScale(Progress, Projectile.ai[2]) : Projectile.ai[2];
        if (!Aura)
        {
            player.heldProj = Projectile.whoAmI;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            // One wave at mid-swing. Only the owner creates networked children.
            if (!released && Kind >= 3 && Progress >= .4f)
            {
                released = true;
                if (Projectile.owner == Main.myPlayer && player.GetModPlayer<LordShotClock>().TryRelease(Main.GameUpdateCount))
                {
                    Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(facing, 0));
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), player.MountedCenter + aim * 26, aim * BladeRouteRules.WaveSpeed,
                        ModContent.ProjectileType<OperatorLordWave>(), Math.Max(1, (int)(Projectile.damage * .8f)), Projectile.knockBack, Projectile.owner, Kind);
                }
            }
            // Place one spike at each sampled point of the swing. Their world
            // positions remain fixed even when the wielder moves afterwards.
            if(Kind<3 && Projectile.owner==Main.myPlayer)
                while(spikes<3 && Progress>=.22f+spikes*.18f)
                {
                    float sample=.22f+spikes*.18f;
                    float angle=BladeRouteRules.Angle(sample,facing,gravity)+player.fullRotation;
                    Vector2 at=Projectile.Center+angle.ToRotationVector2()*(BladeRouteRules.Length(Kind)+25)*Projectile.ai[2];
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),at,Vector2.Zero,ModContent.ProjectileType<OperatorScuuxunSpike>(),
                        Math.Max(1,(int)(Projectile.damage*.35f)),Projectile.knockBack*.3f,Projectile.owner,Kind,angle,facing*gravity);
                    spikes++;
                }
        }
        if (!Main.dedServ && Main.rand.NextBool(2)) BladeRouteArt.Mote(Projectile.Center + Projectile.rotation.ToRotationVector2() * BladeRouteRules.Length(Kind) * Projectile.scale, Kind);
        if (Projectile.localAI[0] >= Math.Max(1, Projectile.ai[1])) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Aura)
        {
            // Swept blade segment prevents high attack speed skipping small targets.
            float previous = BladeRouteRules.Angle(Math.Max(0, Progress - 1 / Math.Max(1, Projectile.ai[1])), facing, gravity) + Main.player[Projectile.owner].fullRotation;
            for (int i = 0; i <= 8; i++)
            {
                Vector2 tip = Projectile.Center + MathHelper.Lerp(previous, Projectile.rotation, i / 8f).ToRotationVector2() * BladeRouteRules.Length(Kind) * Projectile.scale;
                float collision = 0;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, tip, 12 * Projectile.scale, ref collision)) return true;
            }
            return false;
        }
        float cone = Projectile.rotation + MathHelper.TwoPi / 25 * facing * gravity;
        return targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, 94 * Projectile.scale, cone, MathHelper.PiOver4);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind >= 3) BladeRouteArt.Impact(target.Center, Kind);
        if(Kind==6)BladeRouteCombat.FrostleafHit(Main.player[Projectile.owner],target);
        if (Kind < 3 && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)),
                ModContent.ProjectileType<OperatorExecutionSlash>(), Math.Max(1, (int)(Projectile.damage * .35f)), 0, Projectile.owner, Kind, target.whoAmI);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Aura) BladeRouteArt.Aura(Projectile.Center, Projectile.rotation, Projectile.scale, Progress, Kind, facing * gravity,lightColor);
        else BladeRouteArt.Blade(Projectile.Center, Projectile.rotation, BladeRouteRules.Length(Kind) * Projectile.scale, Kind, lightColor);
        return false;
    }
}
public sealed class OperatorBladeAura : OperatorBladeSwing
{
    public override string Texture=>"Terraria/Images/Projectile_"+ProjectileID.TrueExcalibur;
    protected override bool Aura => true;
    public override void DrawBehind(int index,System.Collections.Generic.List<int> behindNPCsAndTiles,System.Collections.Generic.List<int> behindNPCs,
        System.Collections.Generic.List<int> behindProjectiles,System.Collections.Generic.List<int> overPlayers,System.Collections.Generic.List<int> overWiresUI)=>overPlayers.Add(index);
}

public sealed class OperatorExecutionSlash : ModProjectile
{
    private bool visualStarted;
    public override string Texture => "ArknightsMod/Content/Textures/OperatorVfx/LinearSpark";
    public override void SetDefaults()
    {
        Projectile.width = 160; Projectile.height = 12; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 16; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        // The slash stays at the actual hit location. It cannot produce more slashes.
        if (!visualStarted && !Main.dedServ)
        {
            visualStarted = true;
            Color theme = BladeRouteRules.Theme((int)Projectile.ai[0]);
            PhoenixFx.Sakura(Projectile.Center,Projectile.velocity,theme,Color.Lerp(theme,Color.White,.65f));
        }
    }
    public override bool? CanDamage() => Projectile.timeLeft >= 10;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        Vector2 offset = Projectile.rotation.ToRotationVector2() * 80;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - offset, Projectile.Center + offset, 12, ref point);
    }
    public override bool PreDraw(ref Color lightColor)
    { return false; } // Phoenix renders the centered Sakura cut; legacy Slash art is retained.
}

public sealed class OperatorLordWave : ModProjectile
{
    private PhoenixPlasmaClusterFx visual;
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TerraBlade2Shot;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 16; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = 48; Projectile.height = 84;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1; Projectile.timeLeft = BladeRouteRules.WaveLifetime;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        Projectile.localAI[0]++;
        Projectile.rotation = Projectile.velocity.ToRotation();
        if(!Main.dedServ && visual==null)
        {
            visual=new PhoenixPlasmaClusterFx(Projectile,BladePhoenixStyle.For((int)Projectile.ai[0]));
            PhoenixFx.Burst("emitter.player.weapon.69.muzzle",Projectile.Center-Projectile.velocity.SafeNormalize(Vector2.UnitX)*26,
                Projectile.velocity.SafeNormalize(Vector2.UnitX),BladePhoenixStyle.For((int)Projectile.ai[0]));
        }
        Projectile.scale=1;
        // One intact 21-cell plasma cluster; no travelling fork or homing children.
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 side = Projectile.rotation.ToRotationVector2() * 30;
        float width=48+2*Projectile.localAI[0]*BladeRouteRules.WaveSpeed*MathF.Tan(MathHelper.ToRadians(2.5f));
        float point = 0;
        return targetHitbox.Contains(Projectile.Center.ToPoint()) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - side, Projectile.Center + side, width, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        BladePhoenixStyle.Impact(Projectile);
        if((int)Projectile.ai[0]==6)BladeRouteCombat.FrostleafHit(Main.player[Projectile.owner],target);
    }
    public override void OnKill(int timeLeft)=>visual?.Stop();
    public override bool PreDraw(ref Color lightColor)=>false;
}
public sealed class OperatorLordBolt : ModProjectile
{
    private PhoenixMotionFx visual;
    public override string Texture => "ArknightsMod/Content/Textures/OperatorVfx/StarCore";
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 22; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = BladeRouteRules.BoltLifetime;
        Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;
    }
    public override void AI()
    {
        Projectile.localAI[0]++;
        if(!Main.dedServ)visual??=new PhoenixMotionFx(69,Projectile,BladePhoenixStyle.For((int)Projectile.ai[0],.85f));
        if (Projectile.localAI[0] > BladeRouteRules.BoltDelay)
        {
            NPC best = null; float distance = 720 * 720;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < distance && npc.CanBeChasedBy(this) && Collision.CanHitLine(Projectile.position, 12, 12, npc.position, npc.width, npc.height))
                { best = npc; distance = d; }
            }
            if (best != null) Projectile.velocity = Vector2.Lerp(Projectile.velocity, (best.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * BladeRouteRules.WaveSpeed, .075f);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override void OnHitNPC(NPC target,NPC.HitInfo hit,int damageDone)
    {
        BladePhoenixStyle.Impact(Projectile);
        if((int)Projectile.ai[0]==6)BladeRouteCombat.FrostleafHit(Main.player[Projectile.owner],target);
    }
    public override void OnKill(int timeLeft)=>visual?.Stop();
    public override bool PreDraw(ref Color lightColor)=>false;
}

internal static class BladePhoenixStyle
{
    internal static PhoenixFxStyle For(int kind,float scale=1)
    {
        var style=PhoenixFx.Colored(BladeRouteRules.Theme(kind),Color.Lerp(BladeRouteRules.Theme(kind),Color.White,.65f),scale);
        style.MonochromeTexture=true;return style;
    }
    internal static void Impact(Projectile projectile)
    {
        if(!Main.dedServ)PhoenixFx.Burst("emitter.player.weapon.69.impact",projectile.Center,-projectile.velocity.SafeNormalize(Vector2.UnitX),For((int)projectile.ai[0],.7f));
    }
}

public sealed class OperatorScuuxunSpike : ModProjectile
{
    private PhoenixMotionFx visual;
    private Vector2 orbitCenter,spawnOffset;
    private bool orbitReady;
    internal const float InitialAngularSpeed=.18f,AngularDrag=.9f;
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        orbitCenter=Main.player[Projectile.owner].MountedCenter;spawnOffset=Projectile.Center-orbitCenter;orbitReady=true;
        float sign=Projectile.ai[2]==0?1:Math.Sign(Projectile.ai[2]);
        Projectile.velocity=new Vector2(-spawnOffset.Y,spawnOffset.X)*(InitialAngularSpeed*sign);
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(orbitCenter.X);writer.Write(orbitCenter.Y);writer.Write(spawnOffset.X);writer.Write(spawnOffset.Y);writer.Write(Projectile.localAI[0]);writer.Write(Projectile.timeLeft);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        orbitCenter=new(reader.ReadSingle(),reader.ReadSingle());spawnOffset=new(reader.ReadSingle(),reader.ReadSingle());Projectile.localAI[0]=reader.ReadSingle();Projectile.timeLeft=reader.ReadInt32();orbitReady=true;
    }
    public override string Texture=>"Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width=Projectile.height=48;Projectile.friendly=true;Projectile.DamageType=DamageClass.Melee;
        Projectile.penetrate=-1;Projectile.tileCollide=false;Projectile.ignoreWater=true;
        Projectile.timeLeft=BladeRouteRules.SpikeLifetime;Projectile.usesLocalNPCImmunity=true;Projectile.localNPCHitCooldown=-1;
    }
    public override bool ShouldUpdatePosition()=>false;
    public override void AI()
    {
        Projectile.localAI[0]++;
        if(!orbitReady){orbitCenter=Main.player[Projectile.owner].MountedCenter;spawnOffset=Projectile.Center-orbitCenter;orbitReady=true;}
        float sign=Projectile.ai[2]==0?1:Math.Sign(Projectile.ai[2]);
        float angle=sign*InitialAngularSpeed*(1-MathF.Pow(AngularDrag,Projectile.localAI[0]))/(1-AngularDrag);
        Vector2 before=Projectile.Center;
        Vector2 radial=spawnOffset.RotatedBy(angle);
        Projectile.Center=orbitCenter+radial;Projectile.velocity=Projectile.Center-before;
        Projectile.rotation=radial.ToRotation()+sign*MathHelper.PiOver2;
        if(Main.dedServ || visual!=null)return;
        var style=BladePhoenixStyle.For((int)Projectile.ai[0],1.6f);
        style.Opacity=()=>Math.Clamp((BladeRouteRules.SpikeLifetime-Projectile.localAI[0])/18f,0,1);
        style.TrailLifetimeScale=.15f;
        style.HideScuuxunPointer=true;
        visual=new PhoenixMotionFx(67,Projectile,style,()=>Projectile.rotation.ToRotationVector2());
    }
    public override bool? Colliding(Rectangle projHitbox,Rectangle targetHitbox)
    {
        float point=0;Vector2 half=Projectile.rotation.ToRotationVector2()*28;
        return targetHitbox.Contains(Projectile.Center.ToPoint()) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),targetHitbox.Size(),Projectile.Center-half,Projectile.Center+half,18,ref point);
    }
    public override bool PreDraw(ref Color lightColor)=>false;
    public override void OnKill(int timeLeft)=>visual?.Stop();
}
