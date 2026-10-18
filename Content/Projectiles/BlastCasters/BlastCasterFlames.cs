using System;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// Keep the REAL vanilla projectile ID so Terraria draws its actual animated flame,
// growing collision, smoke and tile response. Never replace it with a line or dust cloud.
public sealed class BlastCasterFlames : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool exploded;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.Flames;
    public override void OnSpawn(Projectile projectile, IEntitySource source) => Configure(projectile);
    private static bool Ours(Projectile p, out BlastCasterKind kind, out int mode) => BlastCasterRules.DecodeFlame(p.ai[2], out kind, out mode);
    private static void Configure(Projectile p)
    {
        if (!Ours(p, out _, out _)) return;
        p.penetrate = p.maxPenetrate = -1; p.ArmorPenetration = 0;
        p.tileCollide = false;
        p.GetGlobalProjectile<global::ArknightsMod.Systems.Gameplay.Damage.ArtsProjectileMarker>().IsArtsDamage = true;
    }
    // Vanilla type 85 also checks terrain visibility in Colliding, independently
    // of tileCollide. Override only this branch's marked flames to remove both gates.
    public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
        => Ours(projectile, out _, out _) ? projHitbox.Intersects(targetHitbox) : null;
    public override void AI(Projectile projectile)
    {
        if (!Ours(projectile, out BlastCasterKind kind, out _) || Main.dedServ) return;
        if (projectile.numUpdates == -1 && (int)projectile.localAI[0] % 9 == 0)
            BlastCasterVisuals.FlameWake(projectile, kind);
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Ours(projectile, out BlastCasterKind kind, out int mode)) return;
        // Vanilla itself applies Hellfire for 20s. These are operator/skill additions.
        if (kind == BlastCasterKind.Corroserum) target.AddBuff(BuffID.CursedInferno, 1200);
        if (kind == BlastCasterKind.Corroserum && mode == 2) target.AddBuff(BuffID.Ichor, 180);
        if (kind == BlastCasterKind.Ifrit && mode == 3) target.AddBuff(BuffID.Oiled, 180);
        Vector2 point = Vector2.Clamp(projectile.Center, target.position, target.position + target.Size);
        BlastCasterVisuals.Hit(projectile, target, point, kind);
        if (projectile.owner != Main.myPlayer) return;
        if (kind == BlastCasterKind.Ifrit && mode == 2 && !exploded)
        {
            exploded = true;
            Projectile heat = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), point, Vector2.Zero,
                ModContent.ProjectileType<BlastCasterHeat>(), projectile.damage / 2, 2, projectile.owner, 0);
            heat.CritChance = projectile.CritChance;
        }
        if (kind == BlastCasterKind.Ifrit && mode == 3)
        {
            var owner = Main.player[projectile.owner];
            var runtime = owner.GetModPlayer<BlastCasterPlayer>();
            if (Main.GameUpdateCount >= runtime.NextScorch && owner.ownedProjectileCounts[ModContent.ProjectileType<BlastCasterHeat>()] < 7)
            {
                runtime.NextScorch = Main.GameUpdateCount + 20;
                Projectile heat = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), point, Vector2.Zero,
                    ModContent.ProjectileType<BlastCasterHeat>(), Math.Max(1, projectile.damage / 3), 0, projectile.owner, 1);
                heat.CritChance = projectile.CritChance;
            }
        }
    }
}

// A small flash for Flame Burst, or a bounded lingering patch for Scorched Earth.
public sealed class BlastCasterHeat : ModProjectile
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    private bool initialized;
    public override void OnSpawn(IEntitySource source)
        => Projectile.GetGlobalProjectile<global::ArknightsMod.Systems.Gameplay.Damage.ArtsProjectileMarker>().IsArtsDamage = true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 96; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 120;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 30;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!initialized)
        {
            initialized = true;
            if (Projectile.ai[0] == 0) { Projectile.timeLeft = 12; Projectile.localNPCHitCooldown = -1; }
            BlastCasterVisuals.HeatBurst(Projectile.Center, Projectile.ai[0] == 0);
        }
        if (Projectile.ai[0] == 1 && Projectile.timeLeft % 8 == 0)
            BlastCasterVisuals.Scorch(Projectile.Center);
        Lighting.AddLight(Projectile.Center, .35f * Projectile.timeLeft / 120, .10f * Projectile.timeLeft / 120, .025f);
    }
    public override bool? CanDamage() => Projectile.ai[0] == 0 && Projectile.timeLeft < 10 ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) <= 48 * 48;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 300);
    public override bool PreDraw(ref Color lightColor) => false;
}
