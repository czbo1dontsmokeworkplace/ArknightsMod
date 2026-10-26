using System;
using ArknightsMod.Common.VisualEffects.HelldiverExplosions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;

namespace ArknightsMod.Content.Projectiles.Caster.Gitano;

/// <summary>普攻为局部水晶爆破；命运为一次范围内全目标判定，不叠加多枚爆破。</summary>
public sealed class GitanoBurst : ModProjectile
{
    private const int Lifetime = 20;
    private bool Fate => Projectile.ai[0] > 0f;
    private float Age => Projectile.localAI[0];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 160;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        HelldiverExplosion.Spawn(Projectile, Projectile.Center, 5);
        if (Fate)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 1800;
            Projectile.Center = center;
        }
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<SpreadAfterfield>(), Math.Max(1, (int)(Projectile.damage * .28f)),
                Projectile.knockBack, Projectile.owner, 1f);
    }

    public override bool? CanDamage() => Age <= 2f ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = new(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
            MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
        return Vector2.DistanceSquared(Projectile.Center, closest) <= Projectile.width * Projectile.width * .25f;
    }

    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0f || Main.dedServ) return;
        GitanoVisuals.BurstParticles(Projectile.Center, Fate);
        if (Fate)
        {
            int shown = 0;
            for (int i = 0; i < Main.maxNPCs && shown < 18; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage
                    || Vector2.DistanceSquared(npc.Center, Projectile.Center) > 900f * 900f) continue;
                GitanoVisuals.TargetParticles(npc.Center);
                shown++;
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}
