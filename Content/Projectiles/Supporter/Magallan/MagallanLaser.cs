using System;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanLaser : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Supporter/Magallan/MagallanDrone";
    private readonly float[] samples = new float[3];
    private float length;
    private bool hitTarget;
    private bool Boosted => Projectile.ai[2] > 0;
    private const int Lifetime = 18;
    internal const float MaxLength = 15f * 16f;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2500;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => !Boosted && (hitTarget || target.whoAmI != Math.Abs((int)Projectile.ai[2]) - 1) ? false : null;

    public override void AI()
    {
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        MagallanDrone drone = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.identity == (int)Projectile.ai[0] && p.ModProjectile is MagallanDrone found)
            { drone = found; break; }
        if (drone == null || drone.Module != 1 || drone.Recalling) { Projectile.Kill(); return; }
        Projectile.Center = drone.Muzzle;
        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float reach = Math.Min(MaxLength, Projectile.ai[1]);
        int targetId = Math.Abs((int)Projectile.ai[2]) - 1;
        if (!Boosted && targetId >= 0 && targetId < Main.maxNPCs && Main.npc[targetId].active)
            reach = Math.Min(reach, Vector2.Distance(Projectile.Center, Main.npc[targetId].Center));
        Collision.LaserScan(Projectile.Center, Projectile.velocity, Boosted ? 14f : 5f, reach, samples);
        length = (samples[0] + samples[1] + samples[2]) / 3f;
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.4f, Pitch = 0.2f, MaxInstances = 3 }, Projectile.Center);
        if (!Main.dedServ)
        {
            Vector2 tip = Projectile.Center + Projectile.velocity * length;
            Lighting.AddLight(tip, 0.2f, 0.55f, 0.3f);
            if (Projectile.timeLeft % 6 == 0) MagallanVisuals.LaserVapor(Projectile.Center, tip, Boosted);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity * length, Boosted ? 18f : 6f, ref collisionPoint);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitTarget = true;
        MagallanVisuals.Puff(target.Center, -Projectile.velocity * 0.4f, new Color(151, 200, 174), 28f, 28, 0.4f);
        MagallanVisuals.SparkBurst(target.Center, MagallanVisuals.ModuleColor(1), 3, 1.5f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float age = Lifetime - Projectile.timeLeft;
        float envelope = MathF.Sin((age + 1f) / (Lifetime + 1f) * MathHelper.Pi);
        Vector2 start = Projectile.Center;
        Vector2 end = start + Projectile.velocity * length;
        MagallanVisuals.Beam(start, end, envelope, Boosted);
        return false;
    }
}
