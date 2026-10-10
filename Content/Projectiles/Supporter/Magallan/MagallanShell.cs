using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanShell : ModProjectile
{
    private const float FlightSpeed = 20f;
    private const int StraightFlightTicks = 15;
    public static Vector2 LaunchVelocity => Vector2.UnitY * FlightSpeed;

    internal static float MaxTurnRadians(int flightTicks)
    {
        if (flightTicks <= StraightFlightTicks) return 0f;
        float progress = MathHelper.Clamp((flightTicks - StraightFlightTicks) / 55f, 0f, 1f);
        return MathHelper.Lerp(0.18f, MathHelper.Pi, progress * progress);
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
        Projectile.timeLeft = 210;
    }

    public override bool? CanCutTiles() => false;
    // Collision detonates the shell; only its explosion applies damage or NPC immunity.
    public override bool? CanDamage() => false;

    public override void AI()
    {
        int flightTicks = (int)++Projectile.localAI[0];
        if (flightTicks > StraightFlightTicks)
        {
            NPC target = FindHomingTarget();
            if (target != null)
            {
                float currentAngle = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation();
                float desiredAngle = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).ToRotation();
                float maxTurn = MaxTurnRadians(flightTicks);
                float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -maxTurn, maxTurn);
                Projectile.velocity = (currentAngle + turn).ToRotationVector2() * FlightSpeed;
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
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
        if (flightTicks == 1 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.55f, Pitch = -0.4f, MaxInstances = 3 }, Projectile.Center);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, 0.5f, 0.26f, 0.07f);
        if (!Main.dedServ)
            MagallanVisuals.MissileTrail(Projectile.Center, Projectile.velocity, flightTicks);
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

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<MagallanExplosion>(), Projectile.damage, Projectile.knockBack,
                Projectile.owner);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        MagallanVisuals.Shell(Projectile.Center, Projectile.velocity);
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
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.95f, Pitch = -0.22f, MaxInstances = 4 }, Projectile.Center);
        MagallanVisuals.ExplosionSmoke(Projectile.Center, VisualRadius);
        Player viewer = Main.LocalPlayer;
        if (viewer.active && Vector2.DistanceSquared(viewer.Center, Projectile.Center) < 900f * 900f)
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
                (viewer.Center - Projectile.Center).SafeNormalize(Vector2.UnitY), 4.8f, 4.5f, 6));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float t = 1f - Projectile.timeLeft / 24f;
        MagallanVisuals.Explosion(Projectile.Center, VisualRadius, t);
        return false;
    }
}
