using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Sniper.Schwarz;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// All four wooden-arrow conversions use this class. ai[0]=palette, ai[1]=skill snapshot,
// ai[2]=Schwarz penetration count. No rendering reads the local player's current weapon/skill.
public sealed class CrossbowBolt : ModProjectile
{
    // The rendered core is selected per shot in CrossbowSignatureVfx (Schwarz/Pozemka use native art).
    public override string Texture => "Terraria/Images/MagicPixel";
    internal CrossbowKind Kind => (CrossbowKind)(int)Projectile.ai[0];
    private int age;
    private int returnTarget = -1;
    private ulong lastVisualHitTick = ulong.MaxValue;
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
        ConfigureBallistics();
        var data = Projectile.GetGlobalProjectile<CrossbowShotData>();
        data.Kind = (int)Kind;
        data.Skill = (int)Projectile.ai[1];
    }
    internal void ConfigureBallistics()
    {
        Projectile.extraUpdates = Kind == CrossbowKind.Schwarz ? 3 : Kind == CrossbowKind.Pozemka ? 2 : 1;
        if (Kind == CrossbowKind.Schwarz)
        {
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        if (Kind == CrossbowKind.Pozemka)
        {
            Projectile.penetrate = 2;
            Projectile.localNPCHitCooldown = 30;
        }
        if (Kind == CrossbowKind.Melanite) Projectile.penetrate = Projectile.ai[1] == 2 ? 8 : 6;
        if (Kind == CrossbowKind.Provence) Projectile.penetrate = 9;
        if (Kind == CrossbowKind.BluePoison) Projectile.penetrate = 2;
    }
    public override void AI()
    {
        Projectile.extraUpdates = Kind == CrossbowKind.Schwarz ? 3 : Kind == CrossbowKind.Pozemka ? 2 : 1;
        age++;
        if (Kind == CrossbowKind.Schwarz) Projectile.penetrate = -1;
        else if (Kind == CrossbowKind.Pozemka) UpdateRoseHoming();
        else if (age >= 15 && Kind < CrossbowKind.Melanite) Projectile.velocity.Y = Math.Min(16f, Projectile.velocity.Y + .2f);
        if (Kind >= CrossbowKind.Melanite && age > 150) { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (!Main.dedServ)
        {
            Color color = CrossbowVisuals.ColorFor(Kind);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * .16f);
            CrossbowFlightVfx.Wake(Projectile, Kind);
        }
    }
    public override bool? CanHitNPC(NPC target)
        => Kind == CrossbowKind.Pozemka && Projectile.ai[2] < 0f
            || Kind == CrossbowKind.Provence && Projectile.ai[1] == 2 && target.life > target.lifeMax * .8f ? false : null;

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
        if (Kind == CrossbowKind.Melanite) modifiers.ArmorPenetration += Projectile.ai[1] == 2 ? 20 : 12;
        if (Kind == CrossbowKind.Provence) modifiers.ArmorPenetration += 25;
        if (Kind == CrossbowKind.Provence && Projectile.ai[1] == 1)
            modifiers.SourceDamage *= 1f + .6f * (1f - target.life / (float)Math.Max(1, target.lifeMax));
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Local contact feedback also runs on piercing hits. It cannot deal damage or spawn arrows.
        CrossbowSignatureVfx.Hit(Projectile, Kind);
        lastVisualHitTick = Main.GameUpdateCount;
        if (Kind == CrossbowKind.BluePoison) target.AddBuff(BuffID.Venom, 240);
        if (Kind == CrossbowKind.KroosAlter) target.AddBuff(BuffID.Frostburn, 180);
        if (Kind == CrossbowKind.Pozemka && Projectile.penetrate == 2)
        {
            returnTarget = target.whoAmI;
            Projectile.ai[2] = -30f;
            Projectile.netUpdate = true;
        }
        if (Kind == CrossbowKind.Schwarz)
        {
            // Apply decay to the NEXT target, preserving precision instead of flooring damage every hit.
            Projectile.ai[2]++;
            Projectile.netUpdate = true;
            if (Projectile.owner == Main.myPlayer)
            {
                // 敌人中心是激光中点；随机量只改变天降方向，不能让线路绕过敌人。
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<SchwarzRiftLaser>(),
                    Math.Max(1, (int)(Projectile.damage * .72f)), Projectile.knockBack,
                    Projectile.owner, Main.rand.NextFloat(-.12f, .12f));
            }
        }
    }
    public override void OnKill(int timeLeft)
    {
        if (Kind != CrossbowKind.Schwarz && lastVisualHitTick != Main.GameUpdateCount)
            CrossbowVisuals.SpawnPulse(Projectile.GetSource_Death(), Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitX), Kind, false);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind >= CrossbowKind.Melanite) { CrossbowBranchSkills.Draw(Projectile, Kind); return false; }
        CrossbowVisuals.DrawBolt(Projectile, Kind, age);
        return false;
    }
}
