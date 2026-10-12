using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaCrossBurst : ModProjectile
{
    public override string Texture => MostimaVisuals.Assets + "ChronoCore";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 560;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 44;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanDamage() => Projectile.timeLeft is >= 30 and <= 34 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 p = targetHitbox.ClosestPointInRect(Projectile.Center);
        // The clock face is the blast region; cross pointers mark the same bounded diameter.
        return Vector2.DistanceSquared(p, Projectile.Center) <= 280 * 280;
    }
    public override void AI()
    {
        if (Main.dedServ) return;
        if (Projectile.timeLeft == 44)
        {
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = .72f, Pitch = -.4f }, Projectile.Center);
            MostimaVisuals.Particles(Projectile.Center, 40, 12);
            for (int i = 0; i < 24; i++)
            {
                Vector2 dir = (i * MathHelper.TwoPi / 24).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dir * 40, DustID.BlueCrystalShard,
                    dir * Main.rand.NextFloat(3, 7), 110, MostimaVisuals.Blue, .9f);
                dust.noGravity = true;
            }
        }
        Lighting.AddLight(Projectile.Center, .1f, .3f, .5f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!target.boss && target.realLife < 0) OperatorStunNPC.TryApply(target, 20);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MostimaVisuals.Burst(Projectile.Center, 44 - Projectile.timeLeft);
        return false;
    }
}
