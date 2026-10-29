using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanShell : ModProjectile
{
    internal const int MotionUpdates = 2;
    internal const float IgnitionTick = 24f;
    internal const float IgnitedAcceleration = 15000f; // X-81's 10000, raised 50% on ignition.
    internal const float IgnitedMaxSpeed = 4000f; // Twice the source's activated speed cap.
    private PhoenixMotionFx missileFx;
    private bool ignited;
    public static Vector2 LaunchVelocity => Vector2.UnitY * (200f / 60 / MotionUpdates);
    internal static float SpeedAfterStep(float speed,float ticks)
        => Math.Clamp(speed+(ticks>=IgnitionTick?IgnitedAcceleration:-200f)/(60*MotionUpdates),0,
            ticks>=IgnitionTick?IgnitedMaxSpeed:200f);

    internal static float MaxTurnRadians(int flightTicks)
    {
        return flightTicks < IgnitionTick ? 0f : MathHelper.ToRadians(900f) / 60f;
    }

    public override string Texture => "ArknightsMod/Content/Projectiles/Supporter/Magallan/MagallanDrone";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 10;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 13;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = MotionUpdates-1;
        Projectile.timeLeft = 210*MotionUpdates;
    }

    public override bool? CanCutTiles() => false;
    // Collision detonates the shell; only its explosion applies damage or NPC immunity.
    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile.localAI[0]+=1f/MotionUpdates;
        float ticks=Projectile.localAI[0];
        if(!Main.dedServ)missileFx??=new PhoenixMotionFx(66,Projectile);
        Vector2 heading=Projectile.velocity.SafeNormalize(Vector2.UnitY);
        float speed=SpeedAfterStep(Projectile.velocity.Length()*60*MotionUpdates,ticks);
        if (ticks >= IgnitionTick)
        {
            NPC target = FindHomingTarget();
            if (target != null)
            {
                float currentAngle = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation();
                float desiredAngle = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).ToRotation();
                float maxTurn = MaxTurnRadians((int)ticks)/MotionUpdates;
                float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -maxTurn, maxTurn);
                heading=(currentAngle + turn).ToRotationVector2();
            }
            missileFx?.Activate("actor.player.weapon.66.activation");
            if(!ignited){ignited=true;if(Projectile.owner==Main.myPlayer)Projectile.netUpdate=true;}
        }
        Projectile.velocity=heading*(speed/(60*MotionUpdates));
        Projectile.rotation = Projectile.velocity.ToRotation();
        if(Projectile.owner==Main.myPlayer && Projectile.numUpdates==0 && (int)ticks%6==0)Projectile.netUpdate=true;
        if (Projectile.owner == Main.myPlayer)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile)) continue;
                float hit = 0f;
                if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center,
                    Projectile.Center + Projectile.velocity, 13f, ref hit))
                {
                    Projectile.Kill();
                    return;
                }
            }
        }
        if (ticks <= 1f/MotionUpdates && !Main.dedServ)
            PhoenixFx.Burst("actor.player.weapon.66.muzzle",Projectile.Center,heading);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, 0.5f, 0.26f, 0.07f);
    }

    private NPC FindHomingTarget()
    {
        int id = (int)Projectile.ai[0] - 1;
        if (id >= 0 && id < Main.maxNPCs)
        {
            NPC locked = Main.npc[id];
            if (locked.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(locked.Center, Projectile.Center) < 3200f * 3200f)
                return locked;
        }
        if (Projectile.owner != Main.myPlayer) return null;
        NPC best = null;
        float bestDistance = 2400f * 2400f;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
            if (distance < bestDistance && npc.CanBeChasedBy(Projectile))
            {
                best = npc;
                bestDistance = distance;
            }
        }
        if (best != null)
        {
            Projectile.ai[0] = best.whoAmI + 1;
            Projectile.netUpdate = true;
        }
        return best;
    }

    public override void SendExtraAI(BinaryWriter writer){writer.Write(Projectile.localAI[0]);writer.Write(ignited);}
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float ticks=reader.ReadSingle();ignited=reader.ReadBoolean();
        if(float.IsFinite(ticks))Projectile.localAI[0]=Math.Clamp(ticks,0,210);
    }

    public override void OnKill(int timeLeft)
    {
        missileFx?.Stop();
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<MagallanExplosion>(), Projectile.damage, Projectile.knockBack,
                Projectile.owner,Projectile.velocity.ToRotation());
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // The shared client graph draws the original missile sprite and twin trails.
        return false;
    }
}

public sealed class MagallanExplosion : ModProjectile
{
    private const int BlastSize = 75;
    private const float VisualRadius = BlastSize * 0.5f;
    public override string Texture => "ArknightsMod/Content/Projectiles/Supporter/Magallan/MagallanDrone";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = BlastSize;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 24;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool? CanDamage() => Projectile.timeLeft >= 20 ? null : false;
    public override bool? CanCutTiles() => false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return projHitbox.Intersects(targetHitbox);
    }

    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0 || Main.dedServ) return;
        PhoenixFx.Burst("emitter.player.weapon.66.impact",Projectile.Center,Projectile.ai[0].ToRotationVector2());
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Damage remains this summon explosion; its visuals use X-81's graph.
        return false;
    }
}
