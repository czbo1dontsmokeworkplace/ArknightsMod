using System;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Pozemka;

// Rockrock's outward-then-homing star, adapted for Pozemka's ranged on-hit effect.
// Its initial target guides the flight, but never restricts which enemy it can damage.
public sealed class PozemkaStarBolt : ModProjectile
{
    public override string Texture => ArknightsMod.noTexture;
    private readonly Vector2[] history = new Vector2[22];
    private int historyCount;
    private int Age => 150 - Projectile.timeLeft;

    internal static void SpawnPair(IEntitySource source, Projectile arrow, NPC hitTarget)
    {
        int type = ModContent.ProjectileType<PozemkaStarBolt>();
        int damage = Math.Max(1, (int)MathF.Round(arrow.damage * .6f));
        for (int i = 0; i < 2; i++)
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = Main.rand.NextFloat(72f, 120f);
            Vector2 outward = angle.ToRotationVector2();
            Vector2 start = hitTarget.Center + outward * radius;
            int index = Projectile.NewProjectile(source, start, outward * 6f, type,
                damage, arrow.knockBack * .6f, arrow.owner, hitTarget.whoAmI);
            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = arrow.CritChance;
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 150;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age < 9 ? false : null;

    public override void AI()
    {
        int index = (int)Projectile.ai[0];
        NPC target = index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy(Projectile)
            ? Main.npc[index] : null;
        if (target == null || target.DistanceSQ(Projectile.Center) > 1200f * 1200f)
        {
            target = Projectile.FindTargetWithinRange(1200f, true);
            if (target != null && Projectile.owner == Main.myPlayer && index != target.whoAmI)
            {
                Projectile.ai[0] = target.whoAmI;
                Projectile.netUpdate = true;
            }
        }

        if (target != null)
            Projectile.velocity = RockrockStarBolt.Steer(Projectile.velocity, Projectile.Center,
                target.Center, target.velocity, Age, false);
        else
            Projectile.velocity *= 1.02f;
        Projectile.Center += Projectile.velocity;
        Projectile.rotation = Projectile.velocity.ToRotation();
        for (int i = history.Length - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center;
        historyCount = Math.Min(history.Length, historyCount + 1);
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .22f, .08f, .3f);
            CrossbowSignatureVfx.Wake(Projectile, CrossbowKind.Pozemka);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        return historyCount > 1 && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
            targetHitbox.Size(), history[1], Projectile.Center, 9f, ref point);
    }

    public override void OnKill(int timeLeft)
    {
        CrossbowSignatureVfx.Hit(Projectile, CrossbowKind.Pozemka);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1f, (Age + 1) / 5f);
        PozemkaSalvoTrail.Draw(history.AsSpan(0, historyCount), fade * Projectile.Opacity);
        CrossbowSignatureVfx.DrawRoseSatellite(Projectile, history.AsSpan(0, historyCount), fade);
        return false;
    }
}
