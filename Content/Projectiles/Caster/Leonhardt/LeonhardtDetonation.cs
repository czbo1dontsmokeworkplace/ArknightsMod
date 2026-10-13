using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Buffs.Caster.Leonhardt;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;

namespace ArknightsMod.Content.Projectiles.Caster.Leonhardt;

/// <summary>一次性的圆形法术伤害。绘制寿命与判定寿命分开。</summary>
public sealed class LeonhardtDetonation : ModProjectile
{
    private const int Lifetime = 20;
    private float Charge => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
    private float Radius => Projectile.width * .5f;
    private float Age => Projectile.localAI[0];

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 180;
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
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = Projectile.ai[1] > 0f
            ? 360 : (int)MathHelper.Lerp(116f, 250f, Charge);
        Projectile.Center = center;
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<SpreadAfterfield>(), Math.Max(1, (int)(Projectile.damage * .28f)),
                Projectile.knockBack, Projectile.owner, 2f);
    }

    public override bool? CanDamage() => Age <= 2f ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = new(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
            MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
        return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.ai[1] > 0f)
            target.AddBuff(ModContent.BuffType<LeonhardtFracture>(), 360);
    }

    public override void AI()
    {
        if (Projectile.localAI[0]++ != 0f) return;
        if (Main.dedServ) return;

        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/PramanixDomainHit")
            { Volume = .62f, Pitch = -.24f, MaxInstances = 3 }, Projectile.Center);
        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with
            { Volume = .29f, Pitch = .16f }, Projectile.Center);
        Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX,
            2f + 4f * Charge, 5f, 11));
        LeonhardtVisuals.DetonationParticles(Projectile.Center, Charge);
        for (int i = 0; i < 7; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / 7f + Main.rand.NextFloat(-.11f, .11f))
                .ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(7f, 25f),
                i % 4 == 0 ? DustID.GemDiamond : DustID.GemSapphire,
                direction * Main.rand.NextFloat(3f, 9f) * (.65f + Charge * .55f),
                80, default, Main.rand.NextFloat(.8f, 1.45f));
            dust.noGravity = true;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        LeonhardtVisuals.DrawDetonation(Projectile.Center, Projectile.width,
            MathHelper.Clamp(Age / Lifetime, 0f, 1f));
        return false;
    }
}
