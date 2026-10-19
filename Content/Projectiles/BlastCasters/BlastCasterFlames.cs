using System;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

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
