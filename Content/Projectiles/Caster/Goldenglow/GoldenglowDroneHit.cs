using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// One targeted hit, or one local explosion. Decorative lightning never duplicates this damage.
public sealed class GoldenglowDroneHit : ModProjectile
{
    public override string Texture => "ArknightsMod/Assets/Effects/Goldenglow/LightningImpact";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanHitNPC(NPC target) => Projectile.ai[1] > 0 || target.whoAmI == (int)Projectile.ai[0] ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) <=
        (Projectile.ai[1] > 0 ? 96f * 96f : 16f * 16f);
    public override void AI()
    {
        if (Main.dedServ || Projectile.localAI[0]++ > 0) return;
        int count = Projectile.ai[1] > 0 ? 30 : 4;
        for (int i = 0; i < count; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                Main.rand.NextVector2Circular(Projectile.ai[1] > 0 ? 12f : 3f, Projectile.ai[1] > 0 ? 12f : 3f),
                80, Color.LightSkyBlue, Projectile.ai[1] > 0 ? 1.4f : 0.8f);
            dust.noGravity = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.ai[2] > 0 && !GoldenglowSlowNPC.IsBoss(target)) target.AddBuff(ModContent.BuffType<GoldenglowSlow>(), 30);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
