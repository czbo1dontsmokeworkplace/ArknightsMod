using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.Caster.Skyfire;

/// <summary>真正的陨石弹幕；飞行、撞墙和命中只触发一次范围火焰冲击。</summary>
public sealed class SkyfireMeteor : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Caster/Skyfire/SkyfireMeteor";
    private bool Skyfall => Projectile.ai[0] > 0f;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 28;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 34;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 210;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.velocity *= .5f;
        if (Skyfall)
        {
            Projectile.scale = 2f;
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 68;
            Projectile.Center = center;
        }
    }

    public override void AI()
    {
        if (Projectile.numUpdates == 0) Projectile.velocity *= 1.07f;
        Projectile.localAI[1] += Projectile.velocity.Length();
        if (Projectile.localAI[1] > 2400f) { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, new Vector3(.9f, .33f, .06f));
        if (Projectile.numUpdates == 0) SkyfireVisuals.FlightParticles(Projectile.Center, Projectile.velocity, (int)Projectile.localAI[0]++);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 5 * 60);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Tile collision may zero velocity before OnKill; retain the incoming frame.
        Projectile.velocity = oldVelocity;
        Projectile.Kill();
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity,
                ModContent.ProjectileType<SkyfireImpact>(),
                Math.Max(1, (int)(Projectile.damage * (Skyfall ? 1.75f : 1.4f))),
                Projectile.knockBack, Projectile.owner, Skyfall ? 1f : 0f);
        if (!Main.dedServ)
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with
                { Volume = .52f, Pitch = Skyfall ? -.42f : -.24f }, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        SkyfireVisuals.DrawMeteor(Projectile, Skyfall);
        return false;
    }
}
