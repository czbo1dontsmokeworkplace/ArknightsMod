using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// All four wooden-arrow conversions use this class. ai[0]=palette, ai[1]=skill snapshot,
// ai[2]=Schwarz penetration count. No rendering reads the local player's current weapon/skill.
public sealed class CrossbowBolt : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/KroosAlter/KroosAlterCrossbow_Arrow";
    internal CrossbowKind Kind => (CrossbowKind)(int)Projectile.ai[0];
    private int age;
    private int returnTarget = -1;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(returnTarget);
    public override void ReceiveExtraAI(BinaryReader reader) => returnTarget = reader.ReadInt32();
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 24;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.timeLeft = 600;
        Projectile.aiStyle = -1;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Projectile.extraUpdates = Kind is CrossbowKind.Schwarz or CrossbowKind.Pozemka ? 2 : 1;
        if (Kind == CrossbowKind.Schwarz) Projectile.penetrate = -1;
        if (Kind == CrossbowKind.Pozemka)
        {
            Projectile.penetrate = 2;
            Projectile.localNPCHitCooldown = 30;
        }
        var data = Projectile.GetGlobalProjectile<CrossbowShotData>();
        data.Kind = (int)Kind;
        data.Skill = (int)Projectile.ai[1];
    }
    public override void AI()
    {
        Projectile.extraUpdates = Kind is CrossbowKind.Schwarz or CrossbowKind.Pozemka ? 2 : 1;
        age++;
        if (Kind == CrossbowKind.Schwarz) Projectile.penetrate = -1;
        else if (Kind == CrossbowKind.Pozemka) UpdateRoseHoming();
        else if (age >= 15) Projectile.velocity.Y = Math.Min(16f, Projectile.velocity.Y + .2f);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (!Main.dedServ)
        {
            Color color = CrossbowVisuals.ColorFor(Kind);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * .45f);
            if (age % 2 == 0)
            {
                var dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                    -Projectile.velocity * .12f + Main.rand.NextVector2Circular(1.4f, 1.4f), 60, color, .85f);
                dust.noGravity = true;
            }
        }
    }
    public override bool? CanHitNPC(NPC target)
        => Kind == CrossbowKind.Pozemka && Projectile.ai[2] < 0f ? false : null;

    private void UpdateRoseHoming()
    {
        // Zero means this arrow has never hit an enemy: do not acquire a target or steer.
        if (Projectile.ai[2] == 0f) return;
        // 3 updates per frame: thirty straight-flight updates equal ten game frames.
        // No second damage is allowed during the fly-through, even against another nearby NPC.
        if (Projectile.ai[2] < 0f)
        {
            Projectile.ai[2]++;
            if (Projectile.ai[2] == 0f) Projectile.ai[2] = 1f;
            return;
        }
        NPC target = returnTarget >= 0 && returnTarget < Main.maxNPCs ? Main.npc[returnTarget] : null;
        if (target == null || !target.CanBeChasedBy(Projectile) || target.DistanceSQ(Projectile.Center) > 1200f * 1200f)
            target = Projectile.FindTargetWithinRange(1200f, true);
        if (target == null) return;
        float speed = Projectile.velocity.Length();
        float desired = (target.Center - Projectile.Center).ToRotation();
        float angle = Projectile.velocity.ToRotation().AngleTowards(desired, .09f);
        Projectile.velocity = angle.ToRotationVector2() * speed;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Kind == CrossbowKind.Schwarz ? 12f : 6f, ref point);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == CrossbowKind.Schwarz)
            modifiers.SourceDamage *= CrossbowBallistics.PiercingMultiplier((int)Projectile.ai[2]);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == CrossbowKind.KroosAlter) target.AddBuff(BuffID.Frostburn, 180);
        if (Kind == CrossbowKind.Pozemka && Projectile.penetrate == 2)
        {
            returnTarget = target.whoAmI;
            Projectile.ai[2] = -30f;
            Projectile.netUpdate = true;
            CrossbowVisuals.SpawnPulse(Projectile.GetSource_FromThis(), Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, false);
        }
        if (Kind == CrossbowKind.Schwarz)
        {
            // Apply decay to the NEXT target, preserving precision instead of flooring damage every hit.
            Projectile.ai[2]++;
            Projectile.netUpdate = true;
            CrossbowVisuals.SpawnPulse(Projectile.GetSource_FromThis(), Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, false);
        }
    }
    public override void OnKill(int timeLeft)
        => CrossbowVisuals.SpawnPulse(Projectile.GetSource_Death(), Projectile.Center,
            Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, false);
    public override bool PreDraw(ref Color lightColor)
    {
        CrossbowVisuals.DrawBolt(Projectile, Kind, age);
        return false;
    }
}
